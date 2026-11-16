# Tripo 角色模型（GLB）

- warrior.glb / mage.glb / archer.glb：Tripo v3.1 图生 3D，Mixamo 骨骼，内含动画 idle / run / slash（弓箭手为 shoot）。
- 这里放的是未压缩版（不含 meshopt），Unity 通过 glTFast（已加入 Packages/manifest.json: com.unity.cloud.gltfast）直接导入。
- 注意：slash / shoot 是较长的预设动画，网页版只截取了其中挥砍/拉弓的一段：
  剑士 slash 1.95–2.85s，法师 slash 1.5–2.4s，弓箭手 shoot 0.8–1.8s。
- 模型正面朝 +X，放进场景时需绕 Y 轴旋转 -90°。
