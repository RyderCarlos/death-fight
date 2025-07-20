# 2D格斗游戏系统使用指南

## 系统概述

本系统实现了一个完整的2D横版格斗游戏基础框架，包含以下核心系统：

### 1. 角色移动系统 (PlayerController.cs)
- **前进/后退**: WASD键或方向键
- **跳跃**: 空格键
- **下蹲**: S键或下方向键
- **自动翻转**: 根据移动方向自动调整角色朝向

### 2. 攻击系统 (AttackSystem.cs)
- **轻攻击**: J键或鼠标左键
- **重攻击**: K键或鼠标右键
- **连击系统**: 支持连续攻击形成连击
- **攻击范围检测**: 可视化攻击范围
- **伤害计算**: 不同攻击类型造成不同伤害

### 3. 防御系统 (DefenseSystem.cs)
- **格挡**: Shift键，消耗耐力减少伤害
- **闪避**: Ctrl+空格键，快速位移躲避攻击
- **耐力系统**: 格挡消耗耐力，停止格挡时恢复

### 4. 血量系统 (HealthSystem.cs)
- **血量管理**: 支持受伤、治疗、死亡
- **伤害减免**: 与防御系统联动
- **事件系统**: 血量变化和死亡事件

### 5. 游戏管理器 (GameManager.cs)
- **UI管理**: 血量条、耐力条显示
- **调试信息**: 实时显示角色状态
- **控制说明**: 显示操作指南

### 6. 敌人AI (EnemyAI.cs)
- **自动寻路**: 检测玩家并移动攻击
- **攻击AI**: 在攻击范围内自动攻击
- **可视化范围**: 显示检测和攻击范围

## 设置指南

### 1. 创建玩家角色
1. 创建一个GameObject，命名为"Player"
2. 添加以下组件：
   - Rigidbody2D
   - BoxCollider2D
   - Animator
   - PlayerController
   - AttackSystem
   - DefenseSystem
   - HealthSystem
3. 设置Tag为"Player"
4. 设置Layer为"Player"

### 2. 创建敌人
1. 创建GameObject，命名为"Enemy"
2. 添加组件：
   - Rigidbody2D
   - BoxCollider2D
   - Animator
   - HealthSystem
   - EnemyAI
3. 设置Layer为"Enemy"

### 3. 设置Layer Mask
在PlayerController和AttackSystem中：
- groundMask: 设置为地面层
- enemyMask: 设置为敌人层

### 4. 动画设置
为Animator Controller添加以下参数：
- Speed (Float): 移动速度
- IsGrounded (Bool): 是否在地面
- IsCrouching (Bool): 是否下蹲
- VelocityY (Float): Y轴速度
- LightAttack (Trigger): 轻攻击触发
- HeavyAttack (Trigger): 重攻击触发
- ComboCount (Int): 连击数
- IsAttacking (Bool): 是否攻击中
- IsBlocking (Bool): 是否格挡中
- IsDodging (Bool): 是否闪避中
- Hurt (Trigger): 受伤触发
- Death (Trigger): 死亡触发

## 操作说明

| 操作 | 键位 | 说明 |
|------|------|------|
| 移动 | WASD/方向键 | 左右移动 |
| 跳跃 | 空格键 | 向上跳跃 |
| 下蹲 | S/下方向键 | 降低身形，减速移动 |
| 轻攻击 | J/鼠标左键 | 快速攻击，可连击 |
| 重攻击 | K/鼠标右键 | 强力攻击，伤害高 |
| 格挡 | Shift | 举盾防御，减少伤害 |
| 闪避 | Ctrl+空格 | 快速移动躲避 |

## 扩展建议

1. **添加更多攻击类型**: 上挑、下劈等
2. **技能系统**: 特殊技能和魔法攻击
3. **武器系统**: 不同武器不同属性
4. **状态效果**: 中毒、眩晕等
5. **关卡系统**: 多个战斗场景
6. **多人对战**: 本地或网络多人

## 注意事项

- 确保所有LayerMask设置正确
- 动画Controller需要配置对应的动画状态
- 地面检测需要正确设置GroundCheck位置
- 攻击点位置需要根据角色模型调整