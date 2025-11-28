# Texas Hold'em Poker - 构建指南

## 快速开始

### 方法1: 使用Unity编辑器 (推荐)

1. **安装Unity Hub**
   - 下载: https://unity.com/download

2. **安装Unity 6 (6000.0.23f1)**
   - 打开Unity Hub → Installs → Install Editor
   - 选择 Unity 6.x 版本
   - 勾选模块:
     - Android Build Support (含NDK/SDK)
     - iOS Build Support (macOS)

3. **打开项目**
   ```bash
   # 用Unity Hub打开client目录
   Unity Hub → Projects → Add → 选择 client/ 文件夹
   ```

4. **构建APK**
   - 菜单: `Texas Holdem` → `Build` → `Android APK`
   - 或快捷方式: `File` → `Build Settings` → `Android` → `Build`

5. **获取APK**
   - 输出位置: `client/Builds/TexasHoldem_Android_*.apk`

---

### 方法2: 命令行构建

```bash
# macOS
/Applications/Unity/Hub/Editor/6000.0.23f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -nographics \
    -projectPath ./client \
    -executeMethod TexasHoldem.Editor.BuildScript.BuildAndroidCLI \
    -buildPath ./builds/TexasHoldem.apk \
    -quit

# Windows
"C:\Program Files\Unity\Hub\Editor\6000.0.23f1\Editor\Unity.exe" ^
    -batchmode -nographics ^
    -projectPath .\client ^
    -executeMethod TexasHoldem.Editor.BuildScript.BuildAndroidCLI ^
    -buildPath .\builds\TexasHoldem.apk ^
    -quit

# Linux
~/Unity/Hub/Editor/6000.0.23f1/Editor/Unity \
    -batchmode -nographics \
    -projectPath ./client \
    -executeMethod TexasHoldem.Editor.BuildScript.BuildAndroidCLI \
    -buildPath ./builds/TexasHoldem.apk \
    -quit
```

---

## 构建后端服务器

```bash
# 进入服务器目录
cd server

# 下载依赖
go mod tidy

# 本地运行 (测试)
go run ./cmd/server

# 编译二进制
go build -o ../builds/server ./cmd/server

# 交叉编译Linux版本 (生产环境)
GOOS=linux GOARCH=amd64 go build -o ../builds/server_linux ./cmd/server
```

服务器默认端口: `8080`

---

## 配置说明

### 客户端配置
编辑 `client/Assets/Scripts/Network/NetworkManager.cs`:
```csharp
private const string DefaultServerUrl = "ws://YOUR_SERVER_IP:8080/ws";
```

### 服务器配置
编辑 `server/configs/config.yaml`:
```yaml
server:
  host: "0.0.0.0"
  port: 8080

database:
  host: "localhost"
  port: 5432
  name: "texas_holdem"
  user: "postgres"
  password: "your_password"
```

---

## 测试运行

### 单机模式 (无需服务器)
客户端默认支持离线AI对战,直接运行APK即可测试。

### 联机模式
1. 启动服务器:
   ```bash
   cd server && go run ./cmd/server
   ```
2. 修改客户端服务器地址
3. 重新构建APK
4. 安装到设备测试

---

## 常见问题

### Q: Unity构建报错 "Android SDK not found"
安装Android Build Support模块，或手动设置SDK路径:
`Edit` → `Preferences` → `External Tools` → `Android SDK`

### Q: 构建很慢
首次构建需要编译IL2CPP，约15-30分钟。后续增量构建会快很多。

### Q: APK安装失败
检查设备是否开启"允许安装未知来源应用"。

### Q: 连接服务器失败
1. 确认服务器已启动
2. 确认IP地址和端口正确
3. 检查防火墙设置

---

## 发布清单

- [ ] 更换应用图标 (`Assets/Icons/`)
- [ ] 配置签名证书 (`Player Settings` → `Keystore`)
- [ ] 设置正确的服务器地址
- [ ] 测试离线模式
- [ ] 测试联机模式
- [ ] 性能测试 (内存/帧率)
