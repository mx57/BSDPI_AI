using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BSDPI.Core.Services;

public sealed record WinwsLaunchPlan(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    string SourceProfilePath);

/// <summary>
/// Converts Flowseal/zapret BAT profiles into a direct winws.exe launch.
/// This lets BSDPI become the real parent process of winws.exe instead of cmd.exe/start.
/// </summary>
public static class ProfileBatLauncher
{
    private static readonly Regex BatchVariableRegex = new("%([^%]+)%", RegexOptions.Compiled);

    public static bool TryCreateLaunchPlan(
        string profileBatPath,
        string engineDir,
        out WinwsLaunchPlan? plan,
        out string error)
    {
        plan = null;
        error = string.Empty;

        try
        {
            if (!File.Exists(profileBatPath))
            {
                error = $"BAT не найден: {profileBatPath}";
                return false;
            }

            var profileDir = EnsureTrailingSlash(Path.GetDirectoryName(profileBatPath) ?? engineDir);
            var profileName = Path.GetFileNameWithoutExtension(profileBatPath);
            var variables = BuildInitialVariables(engineDir);

            var commands = MergeCaretContinuations(File.ReadAllLines(profileBatPath, Encoding.UTF8)).ToList();

            // Сохраняем последнюю диагностическую ошибку — возвращаем её только если
            // ни одна из строк BAT не привела к успешному запуску.
            var lastCandidateError = string.Empty;

            foreach (var command in commands)
            {
                if (TryParseSetCommand(command, out var name, out var value))
                {
                    variables[name] = ExpandBatchVariables(value, profileDir, profileName, profileBatPath, variables);
                    continue;
                }

                if (!command.Contains("winws.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                var expandedCommand = ExpandBatchVariables(command, profileDir, profileName, profileBatPath, variables);
                var tokens = SplitCommandLine(expandedCommand);
                var exeIndex = tokens.FindIndex(t => t.Value.EndsWith("winws.exe", StringComparison.OrdinalIgnoreCase));

                if (exeIndex < 0)
                    continue;

                var executablePath = tokens[exeIndex].Value.Replace('\\', Path.DirectorySeparatorChar);
                if (!Path.IsPathRooted(executablePath))
                    executablePath = Path.GetFullPath(Path.Combine(engineDir, executablePath));

                executablePath = Path.GetFullPath(executablePath);
                if (!File.Exists(executablePath))
                {
                    // Файл не найден по этому пути — продолжаем искать в других строках BAT.
                    lastCandidateError = $"winws.exe не найден: {executablePath}";
                    continue;
                }

                var args = tokens
                    .Skip(exeIndex + 1)
                    .Select(t => t.Value)
                    .Where(a => !string.IsNullOrWhiteSpace(a) && a != "^")
                    .ToList();

                // ═══ ИНЪЕКЦИЯ ПОЛЬЗОВАТЕЛЬСКИХ ДОМЕНОВ ИЗ МЕНЕДЖЕРА ═══
                // Автоматически добавляем list-general-user.txt в аргументы winws.exe,
                // чтобы домены из UI BSDPI работали без ручного редактирования BAT-файлов.
                var userHostlistPath = Path.Combine(engineDir, "lists", "list-general-user.txt");
                if (File.Exists(userHostlistPath) && new FileInfo(userHostlistPath).Length > 10)
                {
                    bool hasUserList = args.Any(a => a.Contains("list-general-user", StringComparison.OrdinalIgnoreCase));
                    if (!hasUserList)
                    {
                        args.Add("--hostlist");
                        args.Add(userHostlistPath); // Передаем абсолютный путь
                    }
                }

                var userExcludePath = Path.Combine(engineDir, "lists", "list-exclude-user.txt");
                if (File.Exists(userExcludePath) && new FileInfo(userExcludePath).Length > 10)
                {
                    bool hasExclude = args.Any(a => a.Contains("list-exclude-user", StringComparison.OrdinalIgnoreCase));
                    if (!hasExclude)
                    {
                        args.Add("--hostlist-exclude");
                        args.Add(userExcludePath);
                    }
                }
                // ═══════════════════════════════════════════════════════

                if (args.Count == 0)
                {
                    lastCandidateError = "В BAT найден winws.exe, но не найдены аргументы запуска.";
                    continue;
                }

                var workingDirectory = Path.GetDirectoryName(executablePath) ?? engineDir;
                plan = new WinwsLaunchPlan(executablePath, args, workingDirectory, profileBatPath);
                return true;
            }

            error = lastCandidateError.Length > 0
                ? lastCandidateError
                : "В BAT не найдена команда запуска winws.exe.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static Process StartWinws(WinwsLaunchPlan plan)
    {
        var psi = new ProcessStartInfo
        {
            FileName = plan.ExecutablePath,
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in plan.Arguments)
            psi.ArgumentList.Add(arg);

        var proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start вернул null для winws.exe.");
        _ = Task.Run(() =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bsdpi_winws.log");
                var o = proc.StandardOutput.ReadToEnd();
                var e = proc.StandardError.ReadToEnd();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] winws.exe started (PID {proc.Id})");
                sb.AppendLine($"args: {string.Join(" ", plan.Arguments)}");
                if (!string.IsNullOrWhiteSpace(o)) sb.AppendLine($"STDOUT:\n{o}");
                if (!string.IsNullOrWhiteSpace(e)) sb.AppendLine($"STDERR:\n{e}");
                sb.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] winws.exe exited (code {proc.ExitCode})");
                System.IO.File.AppendAllText(logPath, sb.ToString());
            }
            catch
            {
                // вывод winws недоступен — не критично для работы
            }
        });
        return proc;
    }

    /// <summary>
    /// Загружает драйвер WinDivert перед запуском winws.exe.
    /// winws.exe полагается на работающий драйвер; если драйвер уже загружен, команда
    /// вернёт ошибку «уже запущен» (игнорируется).
    /// ВАЖНО: здесь НЕЛЬЗЯ делать «net stop» перед стартом — это убило бы уже работающий
    /// winws.exe. Особенно критично при сканировании оркестратора, где winws запускается
    /// для каждого профиля: «net stop» дёргал бы драйвер на каждой итерации, роняя текущий
    /// winws и вызывая зависание интерфейса. Выгрузка «призрачных» фильтров после жёсткой
    /// остановки выполняется отдельно в UnloadWinDivertDriver (при Stop).
    /// Вызывается из фонового потока (PrepareRuntimeAsync), чтобы не блокировать UI.
    /// </summary>
    public static void EnsureWinDivertDriver()
    {
        try
        {
            RunNetCommand("net start WinDivert");
        }
        catch
        {
            // Загрузка драйвера не критична сама по себе — winws.exe попытается справиться сам.
        }
    }

    private static void RunNetCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command} >nul 2>&1",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        using var p = Process.Start(psi);
        if (p is not null && !p.WaitForExit(10000))
        {
            try
            {
                p.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Выгружает драйвер WinDivert. После жёсткого завершения winws.exe (Kill / taskkill /F)
    /// драйвер и фильтры остаются активными («призраки»), что мешает следующему запуску.
    /// Вызов из фонового потока.
    /// </summary>
    public static void UnloadWinDivertDriver()
    {
        try
        {
            RunNetCommand("net stop WinDivert");
        }
        catch
        {
            // Драйвер мог быть уже выгружен — это нормально.
        }
    }

    public static void PrepareRuntime(string engineDir)
    {
        EnsureUserListFiles(engineDir);
        TryEnableTcpTimestamps();
    }

    /// <summary>
    /// Полная перезагрузка драйвера WinDivert (stop + start). Используется ТОЛЬКО при
    /// основном запуске zapret пользователем — очищает «призрачные» фильтры, оставшиеся
    /// после жёсткого завершения предыдущего winws.exe (Kill / taskkill /F). НЕ должен
    /// вызываться из оркестратора/сканирования, где winws стартует для каждого профиля,
    /// иначе «net stop» будет ронять уже работающий winws и вызывать зависание UI.
    /// </summary>
    public static void ResetWinDivertDriver()
    {
        // Снимаем «призрачные» winws.exe, оставшиеся от жёсткого завершения предыдущего
        // сеанса (Kill / taskkill /F). Иначе новый winws.exe падает с
        // «A copy of winws is already running».
        try { KillAllWinws(); } catch { }

        // Полный сброс драйвера: выгрузка и повторная загрузка WinDivert. Это единственный
        // способ снять «призрачные» фильтры, которые остаются в драйвере после падения winws
        // и из-за которых новый winws.exe падает через пару секунд (не может забиндить драйвер).
        // Безопасно только в точке Start() (IsRunning == false): там winws гарантированно не
        // работает, поэтому выгрузка драйвера никого не «убьёт» и не создаст гонку.
        try { RunNetCommand("net stop WinDivert"); } catch { }
        try { RunNetCommand("net start WinDivert"); } catch { }
        // Даём драйверу время инициализироваться после перезагрузки, прежде чем winws.exe
        // попытается забиндить WinDivert (иначе возможна гонка и мгновенное падение winws).
        System.Threading.Thread.Sleep(1500);
    }

    private static void KillAllWinws()
    {
        RunNetCommand("taskkill /F /IM winws.exe");
        try
        {
            foreach (var proc in Process.GetProcessesByName("winws"))
            {
                try { proc.Kill(); } catch { }
                finally { proc.Dispose(); }
            }
        }
        catch { }
    }

    /// <summary>
    /// Подготавливает среду выполнения асинхронно (без блокировки UI-потока):
    /// файлы списков, TCP-таймстемпы и, самое важное, корректная перезагрузка драйвера
    /// WinDivert. Между остановкой и повторным запуском winws.exe старый драйвер/фильтры
    /// могут остаться «призраками» (жёсткое завершение winws не снимает фильтры), из-за чего
    /// новый winws.exe падает с «A copy of winws is already running». Поэтому драйвер
    /// сначала принудительно выгружается (net stop), затем загружается заново (net start).
    /// </summary>
    public static Task PrepareRuntimeAsync(string engineDir)
    {
        return Task.Run(() =>
        {
            EnsureUserListFiles(engineDir);
            TryEnableTcpTimestamps();
        });
    }

    public static string ToDisplayCommand(WinwsLaunchPlan plan)
    {
        return $"\"{plan.ExecutablePath}\" {string.Join(" ", plan.Arguments.Select(QuoteIfNeeded))}";
    }

    private static Dictionary<string, string> BuildInitialVariables(string engineDir)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BIN"] = EnsureTrailingSlash(Path.Combine(engineDir, "bin")),
            ["BIN_PATH"] = EnsureTrailingSlash(Path.Combine(engineDir, "bin")),
            ["LISTS"] = EnsureTrailingSlash(Path.Combine(engineDir, "lists")),
            ["LISTS_PATH"] = EnsureTrailingSlash(Path.Combine(engineDir, "lists")),
        };

        var (gameFilter, gameFilterTcp, gameFilterUdp) = ReadGameFilter(engineDir);
        variables["GameFilter"] = gameFilter;
        variables["GameFilterTCP"] = gameFilterTcp;
        variables["GameFilterUDP"] = gameFilterUdp;

        return variables;
    }

    private static (string GameFilter, string GameFilterTcp, string GameFilterUdp) ReadGameFilter(string engineDir)
    {
        var flagPath = Path.Combine(engineDir, "utils", "game_filter.enabled");
        if (!File.Exists(flagPath))
            return ("12", "12", "12");

        var mode = File.ReadLines(flagPath).FirstOrDefault()?.Trim().ToLowerInvariant();
        return mode switch
        {
            "all" => ("1024-65535", "1024-65535", "1024-65535"),
            "tcp" => ("1024-65535", "1024-65535", "12"),
            "udp" => ("1024-65535", "12", "1024-65535"),
            _ => ("1024-65535", "1024-65535", "1024-65535"),
        };
    }

    private static void EnsureUserListFiles(string engineDir)
    {
        try
        {
            var listsDir = Path.Combine(engineDir, "lists");
            Directory.CreateDirectory(listsDir);
            EnsureFile(Path.Combine(listsDir, "ipset-exclude-user.txt"), "203.0.113.113/32");
            EnsureFile(Path.Combine(listsDir, "list-general-user.txt"), "domain.example.abc");
            EnsureFile(Path.Combine(listsDir, "list-exclude-user.txt"), "domain.example.abc");
        }
        catch
        {
            // These files only mirror service.bat load_user_lists behavior. If creation fails,
            // winws.exe will still show the real error in its process exit/status.
        }

        static void EnsureFile(string path, string content)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, content + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static void TryEnableTcpTimestamps()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = "interface tcp set global timestamps=enabled",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            if (process is not null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => process.Dispose();
                if (process.HasExited) process.Dispose();
            }
        }
        catch
        {
            // Non-critical. Flowseal service.bat tries this too, but the profile can still be launched.
        }
    }

    private static IEnumerable<string> MergeCaretContinuations(IEnumerable<string> lines)
    {
        var current = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                if (current.Length > 0)
                    current.Append(' ');
                continue;
            }

            if (line.EndsWith('^'))
            {
                current.Append(line[..^1].TrimEnd());
                current.Append(' ');
                continue;
            }

            current.Append(line);
            yield return current.ToString().Trim();
            current.Clear();
        }

        if (current.Length > 0)
            yield return current.ToString().Trim();
    }

    private static bool TryParseSetCommand(string command, out string name, out string value)
    {
        name = string.Empty;
        value = string.Empty;

        var text = command.Trim();
        if (!text.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            return false;

        var body = text[4..].Trim();
        if (body.StartsWith("/", StringComparison.Ordinal))
            return false;

        if (body.Length >= 2 && body[0] == '"' && body[^1] == '"')
            body = body[1..^1];

        var equalsIndex = body.IndexOf('=');
        if (equalsIndex <= 0)
            return false;

        name = body[..equalsIndex].Trim().Trim('"');
        value = body[(equalsIndex + 1)..].Trim().Trim('"');
        return !string.IsNullOrWhiteSpace(name);
    }

    private static string ExpandBatchVariables(
        string value,
        string profileDir,
        string profileName,
        string profilePath,
        IReadOnlyDictionary<string, string> variables)
    {
        var result = value
            .Replace("%~dp0", profileDir, StringComparison.OrdinalIgnoreCase)
            .Replace("%~n0", profileName, StringComparison.OrdinalIgnoreCase)
            .Replace("%~nx0", Path.GetFileName(profilePath), StringComparison.OrdinalIgnoreCase)
            .Replace("%~f0", profilePath, StringComparison.OrdinalIgnoreCase);

        result = BatchVariableRegex.Replace(result, match =>
        {
            var variableName = match.Groups[1].Value;
            if (variables.TryGetValue(variableName, out var variableValue))
                return variableValue;

            return Environment.GetEnvironmentVariable(variableName) ?? string.Empty;
        });

        return result;
    }

    private sealed record CommandToken(string Value, bool WasQuoted);

    private static List<CommandToken> SplitCommandLine(string commandLine)
    {
        var tokens = new List<CommandToken>();
        var current = new StringBuilder();
        var inQuotes = false;
        var tokenWasQuoted = false;

        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                tokenWasQuoted = true;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                FlushToken();
                continue;
            }

            current.Append(c);
        }

        FlushToken();
        return tokens;

        void FlushToken()
        {
            if (current.Length == 0 && !tokenWasQuoted)
                return;

            tokens.Add(new CommandToken(current.ToString(), tokenWasQuoted));
            current.Clear();
            tokenWasQuoted = false;
        }
    }

    private static string EnsureTrailingSlash(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string QuoteIfNeeded(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return "\"\"";

        return arg.Any(char.IsWhiteSpace) ? $"\"{arg}\"" : arg;
    }
}
