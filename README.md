# Show Your Card

《Slay the Spire 2》队友手牌查看模组。

这个模组会在战斗界面中显示一个可拖动的覆盖层，用于实时查看队友当前手牌。

## 功能

- 实时读取 `CombatState.Players`
- 显示每位队友当前手牌
- 显示卡牌费用与标题
- 高亮当前行动中的队友
- 支持折叠与拖动窗口

## 技术实现

- 使用 Harmony 给游戏 `Hook` 打补丁
- 监听 `BeforeCombatStart`
- 监听 `AfterCombatEnd`
- 监听 `AfterPlayerTurnStart`
- 监听若干卡牌移动相关 Hook 以刷新队友手牌
- 使用 Godot `CanvasLayer` 渲染覆盖层 UI

## 使用前配置

构建前需要先修改 `ShowYourCard.csproj` 中的游戏目录：

```xml
<Sts2Dir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2</Sts2Dir>
```

如果你的游戏不在这个位置，请改成自己的安装目录。

## 构建

在项目目录执行：

```powershell
dotnet build
```

构建成功后会生成 DLL，并自动尝试复制到游戏目录下的 `mods\ShowYourCard\`。
