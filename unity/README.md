# 萌域传说（Chibi Realm Legends）

Q 版 3D 动作角色扮演游戏原型。用 Unity 2022.3 LTS **内置渲染管线（Built-in）** 开发，目标是导出 WebGL，在手机浏览器（尤其是 **Android Chrome**）里游玩。角色、建筑、特效全部由基础几何体拼成，不依赖外部模型；卡通描边着色器兼容 WebGL。

打开工程后点一个菜单即可生成「青岚镇」演示场景。若你只想先感受手感与美术，也可直接用仓库外的 `preview/index.html`（three.js 预览）。

## 需要什么

- Unity **2022.3 LTS**（工程版本写的是 2022.3.50f1，任意 2022.3 版本打开后会自动升级/兼容）
- 模块：WebGL Build Support（若要打包到手机浏览器）
- 一台能开本地静态服务器的电脑（手机请用 HTTP 访问，不要直接用 `file://`）

本原型没有在此环境完成真实的 Unity 编译/WebGL 出包（编辑器需要登录许可证）。脚本按 2022.3 / C# 9 编写，打开工程后由 Unity 导入即可。

## 第一次打开

1. 用 Unity Hub 打开 `chibi-arpg-unity` 文件夹。
2. 等待脚本与着色器编译完成。
3. 菜单栏选择 **萌域传说 → 生成演示场景（青岚镇）**。  
   场景会保存为 `Assets/Scenes/QinglanTown.unity`，并加入 Build Settings。
4. 按下 Play。会先进入标题/职业选择界面。

如果忘了点菜单、直接在空场景里按了 Play，运行时也会自动搭建同样的世界。

## 怎么玩

- **移动**：左下角虚拟摇杆；电脑上可用 WASD / 方向键。
- **攻击**：右下角按钮，或键盘 `空格` / `J`。
- **技能**：技能按钮，或键盘 `K` / `Q`（消耗法力）。
- **交谈**：靠近 NPC 后点「交谈」，或按 `E` / `F`。
- **剑士（推荐，默认高亮）**：三段近战连击，带顿帧、击退和镜头震动；技能「旋斩」。高生命。
- **法师**：远程魔弹；技能「魔爆」范围伤害。高法力、低生命。
- **弓箭手**：快速射箭；技能扇形「连射」。属性均衡。

故事发生在原创小镇 **青岚镇**。蚀雾从镇外的 **雾石坡** 蔓延而来，请依次完成开场任务链（对话 → 击败蚀斑史莱姆 → 收集蚀核碎片并对抗大型蚀雾史莱姆），向镇长苏晚晴复命。任务、金币和经验用 `PlayerPrefs` 本地保存；标题界面若有存档可点「继续冒险」。

角色倒下后可以「回到小镇」在古井广场复活，任务进度会保留。

## 打包 WebGL 并在手机上打开

1. 菜单 **萌域传说 → 构建 WebGL（手机浏览器 / Android）**。  
   会自动套用面向移动端的设置，并输出到工程内 `Build/WebGL/`。
2. 本地测试若遇到压缩包无法加载，改用 **萌域传说 → 构建 WebGL（无压缩，便于本地测试）**。正式构建已开启 **Gzip + 解压回退**，普通静态服务器也可以用。
3. 在输出目录启动本地服务，例如：

```bash
cd Build/WebGL
python3 -m http.server 8080
```

4. 电脑浏览器打开 `http://localhost:8080`。  
   手机请与电脑同一局域网，访问 `http://电脑IP:8080`。建议用 **Android Chrome**，横屏游玩。
5. 页面右上角有「全屏」按钮。模板已关闭下拉刷新、双指缩放，并把 `devicePixelRatio` 限制在 1.5 以内，减轻中端安卓的压力。

### 构建菜单里已经做的 Android 优化

- 内存几何增长（约 128–1024 MB），避免一次性要过大堆
- 关闭 WebGL 异常支持，减小运行时开销
- 默认关闭硬阴影、降低像素灯（中端机更稳）
- 使用自定义模板 `ChibiMobile`：`touch-action: none`、`overscroll-behavior: none`、禁止缩放、全屏按钮
- Gamma 色彩空间，卡通描边在手机上更稳定

若仍卡顿：在手机 Chrome 里关掉省电限制，或使用「无压缩」包并降低画质档。

## 工程里有什么

- `Assets/Scripts`：玩家、战斗、敌人、任务、NPC、UI、场景生成
- `Assets/Scripts/Class`、`Assets/Scripts/Quest`：职业与任务的 ScriptableObject 定义（也可由菜单写入 `Assets/Resources/Data`）
- `Assets/Shaders/ToonOutline.shader`：两Pass 卡通光 + 描边 + 清新边缘光，WebGL 可用
- `Assets/Editor`：生成场景、WebGL 构建菜单
- `Assets/WebGLTemplates/ChibiMobile`：手机浏览器页面模板

渲染管线是 **Built-in**，不是 URP。自定义卡通着色器按内置 ForwardBase 编写，导入 URP 需要另做适配。

## 下一步（把原型换成真正的 Q 版模型）

1. 用 Meshy、Tripo、Rodin 等 AI 3D 工具，或从 Asset Store 购买 Q 版角色，替换 `ChibiFactory` 拼出的几何体。建议保留大头、短身体的比例，以及设定中的配色：
   - 剑士：银蓝铠甲、红披风、棕发、蓝圆盾金字纹
   - 法师：白紫长袍、大尖帽、紫发、金杖托蓝珠
   - 弓箭手：带耳尖的绿风帽、奶油色衬衣与棕皮甲、木弓与箭袋
2. 把小镇的白墙彩顶小屋、条纹摊位、石拱门换成精细模型，地面铺真正的石板贴图。
3. 为角色加上 Animator 与攻击动画（剑士连段尤其值得打磨）。
4. 用 NavMesh 或更完整的 AI 让史莱姆在雾石坡里绕行。
5. 再补音效、音乐、背包与更多任务。

预览页 `preview/index.html` 只用于提前体验手感与清新 Q 版氛围，正式内容以 Unity 工程为准。


## 用手机直接打开 three.js 预览

仓库旁的 `preview/index.html` 是同一套玩法与美术的轻量预览（触摸摇杆、职业选择、青岚镇与任务）。请用本地 HTTP 服务打开，不要用 `file://`。

若要把预览发成手机浏览器可点的公开 https 链接（无需注册），可把整个 `preview` 文件夹（需包含 `index.html` 与 `three.min.js`）拖到 [Netlify Drop](https://app.netlify.com/drop)，几秒后会得到 `https://xxxx.netlify.app` 地址，用 Android Chrome 打开即可。页面已适配下拉刷新、双指缩放，并有全屏按钮。
