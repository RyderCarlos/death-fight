# Deathfight 战斗系统实现指南

## 系统概述

本项目实现了一个完整的2D战斗游戏系统，包含以下核心功能：

### 1. 攻击系统 (AttackSystem)
- **文件路径**: `Assets/Scripts/Combat/Attack/AttackSystem.cs`
- **功能特点**:
  - 支持多种攻击类型（轻拳、重拳、轻腿、重腿、特殊技能）
  - 攻击检测系统（圆形、矩形检测）
  - 攻击序列管理（前摇、判定、后摇）
  - 攻击队列系统
  - 能量消耗和获得机制

### 2. 防御系统 (DefenseSystem)
- **文件路径**: `Assets/Scripts/Combat/Defence/DefenseSystem.cs`
- **功能特点**:
  - 格挡机制（普通格挡、完美格挡）
  - 闪避系统（无敌帧、移动）
  - 防御破解机制
  - 反击窗口系统
  - 能量消耗和获得

### 3. 连击系统 (ComboSystem)
- **文件路径**: `Assets/Scripts/Combat/Combo/ComboSystem.cs`
- **功能特点**:
  - 连击检测和计数
  - 连击倍数系统
  - 预定义连击序列
  - 输入缓冲系统
  - 连击时间窗口管理

### 4. 血量系统 (HealthSystem)
- **文件路径**: `Assets/Scripts/Combat/Health/HealthSystem.cs`
- **功能特点**:
  - 血量管理和显示
  - 伤害计算和处理
  - 暴击系统
  - 无敌时间机制
  - 血量恢复系统
  - 死亡和复活处理

### 5. 能量系统 (EnergySystem)
- **文件路径**: `Assets/Scripts/Combat/Energy/EnergySystem.cs`
- **功能特点**:
  - 能量积累和消耗
  - 特殊技能阈值管理
  - 能量自动恢复
  - 多种能量获得方式

### 6. UI系统
- **血量条**: `Assets/Scripts/UI/HealthBar.cs`
- **能量条**: `Assets/Scripts/UI/EnergyBar.cs`
- **连击显示**: `Assets/Scripts/UI/ComboDisplay.cs`
- **综合HUD**: `Assets/Scripts/UI/CombatHUD.cs`

## 快速开始

### 1. 基本设置

1. **创建玩家对象**:
   - 创建一个空的GameObject，命名为"Player"
   - 添加以下组件：
     - `PlayerController` (移动控制)
     - `AttackSystem` (攻击系统)
     - `DefenseSystem` (防御系统)
     - `ComboSystem` (连击系统)
     - `HealthSystem` (血量系统)
     - `EnergySystem` (能量系统)
     - `SpecialSkillController` (特殊技能)

2. **配置攻击数据**:
   - 在Project窗口右键 → Create → 战斗游戏 → 攻击数据
   - 配置攻击参数（伤害、范围、时间等）
   - 将攻击数据赋值给AttackSystem的`availableAttacks`数组

3. **创建连击数据**:
   - 在Project窗口右键 → Create → 战斗游戏 → 连击数据
   - 配置连击序列和时间窗口
   - 将连击数据赋值给ComboSystem的`comboSequences`数组

### 2. 输入系统配置

编辑Unity的Input Manager，添加以下输入：
- 轻拳：J键
- 重拳：U键
- 轻腿：K键
- 重腿：I键
- 特殊技能：E键
- 格挡：L键
- 闪避：L键（双击或组合键）

### 3. UI设置

1. **创建Canvas**:
   - 创建UI Canvas
   - 添加CombatHUD组件

2. **配置UI组件**:
   - 创建血量条UI并配置HealthBar组件
   - 创建能量条UI并配置EnergyBar组件
   - 创建连击显示UI并配置ComboDisplay组件

## 高级功能

### 1. 自定义攻击类型

```csharp
// 在AttackData.cs中添加新的攻击类型
public enum AttackType
{
    轻拳 = 0,
    重拳 = 1,
    轻腿 = 2,
    重腿 = 3,
    特殊技能 = 4,
    // 添加新类型
    投技 = 5,
    反击 = 6
}
```

### 2. 创建自定义连击序列

```csharp
// 在ComboData ScriptableObject中配置
attackSequence = new AttackType[] { 
    AttackType.轻拳, 
    AttackType.重拳, 
    AttackType.轻腿 
};
timingWindows = new float[] { 0.5f, 0.3f }; // 每个攻击之间的时间窗口
```

### 3. 扩展伤害计算

```csharp
// 在DamageInfo.cs中添加新的伤害修正
public void CalculateFinalDamage()
{
    finalDamage = damage * damageMultiplier;
    
    // 自定义修正
    if (isBlocked)
        finalDamage *= 0.5f;
    
    if (isCritical)
        finalDamage *= 1.5f;
    
    // 添加新的修正逻辑
    if (isCounterAttack)
        finalDamage *= 2.0f;
}
```

## 系统配置参数

### AttackSystem 主要参数:
- `globalCooldown`: 全局攻击冷却时间
- `canCancelAttack`: 是否可以取消攻击
- `availableAttacks`: 可用攻击数据数组

### DefenseSystem 主要参数:
- `blockDamageReduction`: 格挡伤害减免比例
- `perfectBlockWindow`: 完美格挡时间窗口
- `dodgeInvincibilityTime`: 闪避无敌时间
- `dodgeCooldown`: 闪避冷却时间

### ComboSystem 主要参数:
- `comboResetTime`: 连击重置时间
- `inputBufferTime`: 输入缓冲时间
- `damageIncreaseRate`: 连击伤害递增率
- `maxComboMultiplier`: 最大连击倍数

### HealthSystem 主要参数:
- `maxHealth`: 最大血量
- `invincibilityTime`: 无敌时间
- `autoRegeneration`: 是否自动恢复血量
- `criticalChance`: 暴击概率

### EnergySystem 主要参数:
- `maxEnergy`: 最大能量
- `energyRegenRate`: 能量恢复速率
- `specialSkillThreshold`: 特殊技能阈值

## 事件系统

各个系统都提供了丰富的事件接口，可以用于扩展功能：

```csharp
// 攻击系统事件
attackSystem.OnAttackStart += (attackData) => { };
attackSystem.OnHitTarget += (target, attackData) => { };

// 防御系统事件
defenseSystem.OnBlock += (damageInfo) => { };
defenseSystem.OnPerfectBlock += (damageInfo) => { };

// 连击系统事件
comboSystem.OnComboStart += (comboCount) => { };
comboSystem.OnComboExtend += (comboCount, multiplier) => { };

// 血量系统事件
healthSystem.OnTakeDamage += (damageInfo) => { };
healthSystem.OnDeath += () => { };

// 能量系统事件
energySystem.OnEnergyChanged += (current, max) => { };
energySystem.OnSpecialSkillAvailable += (available) => { };
```

## 调试和测试

1. **启用调试信息**:
   - 在各个系统中启用调试模式
   - 查看Console输出了解系统状态

2. **使用DebugUI**:
   - 添加DebugUI组件查看实时数据
   - 手动触发各种事件进行测试

3. **性能优化**:
   - 合理设置更新频率
   - 使用对象池管理特效
   - 优化碰撞检测范围

## 常见问题

### Q: 攻击没有造成伤害？
A: 检查目标对象是否有HealthSystem组件，以及LayerMask设置是否正确。

### Q: 连击无法触发？
A: 检查连击时间窗口设置，确保输入在有效时间内。

### Q: UI不显示？
A: 确保UI组件正确连接到对应的系统，并检查Canvas设置。

### Q: 能量不恢复？
A: 检查能量恢复设置，确保`autoRegeneration`为true且`energyRegenRate`大于0。

## 扩展建议

1. **添加更多攻击类型**（空中攻击、蓄力攻击等）
2. **实现状态效果系统**（中毒、冰冻、燃烧等）
3. **添加武器系统**（不同武器有不同攻击数据）
4. **实现AI对手**（使用相同的战斗系统）
5. **添加网络多人支持**（同步战斗状态）

---

*此系统提供了完整的战斗游戏框架，可以根据具体需求进行扩展和定制。*