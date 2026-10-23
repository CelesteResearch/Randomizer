[English](README.md) | [简体中文](README.zh-CN.md)

# Randomizer

Randomizer port for the Shared runtime. Workspace location: `Mods/Randomizer/`.

Projects:

- `Celeste.Mods.Randomizer.csproj`
- `Generation/Celeste.Mods.Randomizer.Generation.csproj`

Repository dependencies: **Shared, TAS**. Prepare Shared resources using the [Tools README][tools-readme], then build the main project:

```powershell
dotnet build ./Mods/Randomizer/Celeste.Mods.Randomizer.csproj -c Release
```

License for the code and port-specific changes: **MIT**. See [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md) for terms and third-party notices.

[tools-readme]: https://github.com/CelesteResearch/Tools/blob/main/README.md
