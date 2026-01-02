# Project Arc 开发计划书 (Updated)

## 1. 项目概况

* 核心玩法：右下角扇形区域操作的弹幕射击/防守游戏。
* 技术栈：Unity 2022+ (URP), VFX Graph, Shader Graph.
* 当前阶段：核心交互原型 (Core Interaction Prototype) 已跑通。

## 2. 进度追踪 (Status Tracker)

### ✅ 已完成 (Completed)

#### Core 基础设施

* 架构搭建：完成了 Assets/_Game 的目录结构与命名空间规划。
* GameManager：实现了基本的游戏状态机 (Boot, Menu, Gameplay, GameOver)。
* ObjectPoolManager：实现了高性能对象池，支持自动扩容，用于子弹和特效的高频生成。
* EventManager：实现了轻量级事件总线，用于解耦各系统。

#### Player 核心交互

* TurretController：
  * 实现了炮台的平滑旋转。
  * [关键优化] 解决了跨越 0°/360° 轴线时的角度回绕 (Angle Wrapping) 问题 (Smart Clamp)。
  * 实现了基于 MinAngle (270°) 和 MaxAngle (360°) 的扇形限制。

* WeaponSystem：
  * 实现了基于对象池的发射逻辑。
  * 支持多枪口配置。
  * 支持手动/自动开火切换接口。

* Input System (World Space)：
  * 废弃了 2D UI 方案，转向更沉浸的 World Space UI。
  * 实现了 WorldSpaceController，通过 3D 射线检测 ControlPad 来驱动炮台，手感更直观。

## 3. 下一步计划 (Next Steps)

### 🚀 阶段 3：视觉反馈与表现 (TA 重点)

目标：利用 Shader 和 VFX 提升操作反馈，摆脱“方块”感。

* **[P0] 操作盘 Shader (Control Pad Visualization)**
  * 制作 SG_ControlPad：
  * 在 3D 平面上动态绘制扇形区域。
  * 交互反馈：当手指按下时，点击位置产生高亮波纹或 grid 变形效果。
  * 范围指示：清晰显示当前的攻击角度限制。

* **[P1] 子弹与打击感 VFX**
  * 使用 VFX Graph 制作子弹拖尾 (Trail)。
  * 制作命中 (Hit) 和开火 (Muzzle Flash) 的粒子特效。

* **[P2] 护盾系统 (Visuals)**
  * 制作 SG_Shield：基于 Fresnel 和顶点位移的能量盾材质。
  * 实现受击时的局部波纹扩散效果（通过 C# 传递受击点给 Shader）。

### ⚔️ 阶段 4：战斗闭环 (Gameplay Loop)

目标：引入敌人，建立“攻击-防御-资源”循环。

* [P0] 敌人系统基础
  * EnemyController：基本的移动逻辑（贝塞尔曲线轨迹）。
  * Damage System：引入 IDamageable 接口，处理子弹对敌人、敌人对据点的伤害。

* [P1] 敌方弹幕
  * 实现敌人的弹幕发射器（Emitters）。
  * 核心机制：实现子弹抵消 (Bullet Cancellation) 逻辑（我方子弹碰撞敌方子弹）。

* [P2] 防御技能
  * 实现“主防御”逻辑：短时间无敌 + 反弹弹幕。

## 4. 待定/远期规划 (Backlog)

* 充能武器 (Ultimate)：全屏激光或时间静止效果。
* 局外成长：武器升级、科技树 UI。
* 关卡配置：基于 ScriptableObject 的波次编辑器。