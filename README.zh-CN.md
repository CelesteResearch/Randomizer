[English](README.md) | [简体中文](README.zh-CN.md)

# Randomizer

适用于 Shared 运行时的 Randomizer 移植。工作区位置：`Mods/Randomizer/`。

项目：

- `Celeste.Mods.Randomizer.csproj`
- `Generation/Celeste.Mods.Randomizer.Generation.csproj`

依赖仓库：**Shared, TAS**。先按 [Tools README][tools-readme-zh-cn] 准备 Shared 资源，再构建主项目：

```powershell
dotnet build ./Mods/Randomizer/Celeste.Mods.Randomizer.csproj -c Release
```

代码及移植修改许可：**MIT**。许可条款及第三方声明见 [LICENSE](LICENSE) 和 [NOTICE.md](NOTICE.md)。

[tools-readme-zh-cn]: https://github.com/CelesteResearch/Tools/blob/main/README.zh-CN.md
