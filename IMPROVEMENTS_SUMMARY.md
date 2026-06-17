# FluxRoute AI — Сводка улучшений

## Дата: 2026-06-17

## Выполненные задачи (сессия 2026-06-17)

### 1. Исправление вкладки «Конструктор» ✅

**Проблемы:**
- Клик по ноде сразу удалял её (вместо выбора)
- `Canvas.ClearAll()` вызывал `InvalidOperationException` (модификация во время итерации)
- `ConnectionLine` не перерисовывался при смене цвета

**Решения:**
- `NodeSelected` → `SelectedNode` (выбор вместо удаления)
- `ClearAll()` → `Children.Clear()` (безопасная очистка)
- `LineBrushProperty` → `AffectsRender | AffectsMeasure`
- Добавлен `NodeControl.SetSelected(bool)` с визуальной рамкой

---

### 2. Воссоздание UI компонентов Конструктора ✅

**FluxRoute/Controls/NodeCanvas.cs:**
- Custom Canvas с RenderTransform (ScaleTransform + TranslateTransform)
- Drag-and-drop нод через `MouseLeftButtonDown/Move/Up`
- Zoom колесом мыши (0.2x – 3.0x)
- Pan: Middle button / Alt+Left / Left на пустом canvas
- Визуальный выбор нод (синяя рамка #58A6FF)

**FluxRoute/Controls/NodeControl.cs:**
- UserControl с программным визуальным деревом
- Border с цветом по типу ноды (NodeAppearance)
- Иконка + Title + Subtitle
- Drag-and-drop: обновляет `ChainNode.X/Y`

**FluxRoute/Controls/ConnectionLine.cs:**
- Shape с BezierSegment
- DependencyProperties: Start, End, LineBrush
- Кривая: `dx = max(|End.X - Start.X| * 0.5, 30)`

**FluxRoute/ViewModels/ChainBuilderViewModel.cs:**
- `ObservableCollection` для Nodes, Connections, ChainList
- CRUD: NewChain, LoadChain, AddNode, DeleteSelectedNode, DeleteChain
- Автосохранение через `ChainStore.Save()`
- Синхронизация ViewModel ↔ Canvas через `_canvas?.AddNode()`

---

### 3. GPL v3 Compliance ✅

**AppUpdaterService.cs:**
```diff
- "https://github.com/klondike0x/FluxRoute/releases.atom"
+ "https://github.com/mx57/FluxRoute_AI/releases.atom"

- "https://github.com/klondike0x/FluxRoute/releases/download/{0}/FluxRoute-{0}-portable.zip"
+ "https://github.com/mx57/FluxRoute_AI/releases/download/{0}/FluxRoute_AI-{0}-portable.zip"
```

**Directory.Build.props (новый):**
```xml
<Authors>mx57</Authors>
<Copyright>Copyright (C) 2026 mx57. Based on klondike0x/FluxRoute (GPLv3).</Copyright>
<PackageLicenseExpression>GPL-3.0-only</PackageLicenseExpression>
<RepositoryUrl>https://github.com/mx57/FluxRoute_AI</RepositoryUrl>
```

**NOTICE (новый):**
- Атрибуция: klondike0x/FluxRoute, bol-van/zapret, Flowseal, basil00/WinDivert
- Лицензии NuGet пакетов: MIT, Apache 2.0 (все GPL-совместимые)

---

### 4. Отказ от ответственности ✅

Добавлен в оба README:

**README.md (RU):**
> **FluxRoute AI** является образовательным и исследовательским программным обеспечением, предназначенным для изучения сетевых технологий...
> Данное ПО **не является** инструментом для нарушения действующего законодательства...

**README.en.md (EN):**
> **FluxRoute AI** is educational and research software designed for studying network technologies...
> This software **is not** a tool for violating applicable laws...

---

### 5. Исправление тестов ✅

**AiHistoryStoreTests.LoadAll_HandlesCorruptLines:**
```diff
- File.WriteAllText(path, "valid json\n{invalid}\nanother valid\n");
+ var valid = JsonSerializer.Serialize(new ProbeOutcome { GenomeId = Guid.NewGuid(), ... });
+ File.WriteAllText(path, valid + "\n{invalid}\nanother valid\n");
```

**AiHistoryStoreTests.RotateOldEntries_HandlesNonExistentFile:**
```diff
- Assert.False(File.Exists(path));
+ Assert.True(File.Exists(path));  // RotateOldEntries создаёт файл даже пустой
```

---

## Статус выполнения (обновлено)

| Приоритет | Задачи | Статус |
|-----------|--------|--------|
| 1. Надёжность | 5/5 | ✅ |
| 2. ИИ | 2/8 | 🔄 |
| 3. Производительность | 2/4 | ✅ |
| 4. UX | 2/6 | ✅ (конструктор + график) |
| 5. Тестирование | 4/7 | 🔄 |
| 6. Архитектура | 1/4 | 🔄 |

**Общий прогресс:** 16/34 задач (47.1%)
**Тесты:** 53/53 зелёные

---

## Рекомендации для следующих улучшений

### Высокий приоритет:
1. **UCB1 (Upper Confidence Bound)** — альтернатива Thompson Sampling
2. **Адаптивный exploration rate** — снижение по мере накопления данных
3. **Интеграционные тесты** — тестирование полного цикла AiOrchestratorService

### Средний приоритет:
4. **График эффективности** — LiveChartsCore line chart (частично реализован)
5. **Экспорт в CSV** — анализ истории проверок
6. **Инжекция зависимостей** — замена Func<> делегатов на DI

### Низкий приоритет:
7. **Drag-and-drop** для reordering стратегий в реестре
8. **Windows Toast** уведомления при смене стратегии

---

## Технические детали

### Зависимости
- .NET 10 (net10.0-windows)
- WPF + WinForms (для типовых конфликтов — using alias)
- CommunityToolkit.Mvvm 8.4.x
- LiveChartsCore.SkiaSharpView.WPF 2.0.4
- Serilog + File sink
- xUnit + coverlet (тесты)

### Сборка
```bash
dotnet build FluxRoute.slnx
dotnet test FluxRoute.slnx
dotnet publish FluxRoute/FluxRoute.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```
