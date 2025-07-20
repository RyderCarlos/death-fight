# Enemy不移动问题解决指南

## 常见问题和解决方法

### 1. 检查必需组件
Enemy GameObject必须包含以下组件：
- **Rigidbody2D** ✓
- **Collider2D** (BoxCollider2D或其他) ✓  
- **EnemyAI** 脚本 ✓
- **HealthSystem** 脚本 ✓
- **EnemyDebugger** 脚本 (用于调试)

### 2. Rigidbody2D设置检查
```
Rigidbody2D设置：
- Body Type: Dynamic
- Material: 无或适当的Physics Material 2D
- Mass: 1 (默认)
- Linear Drag: 0-1 (推荐0.5)
- Angular Drag: 0.05 (默认)
- Gravity Scale: 1 (如果需要重力)
- Freeze Rotation Z: ✓ (勾选，防止旋转)
```

### 3. Player检测问题
确保Player设置正确：
- Player GameObject的Tag设置为 **"Player"**
- 或者Player有 **PlayerController** 组件

### 4. Layer和碰撞设置
- Enemy和Player应该在不同的Layer
- 检查Physics2D设置中的Layer Collision Matrix
- 确保Enemy和Ground可以碰撞

### 5. EnemyAI参数调整
```csharp
// 在Inspector中调整这些值
detectionRange = 10f;  // 增大检测范围
attackRange = 1.5f;    // 攻击范围
moveSpeed = 3f;        // 增加移动速度
```

## 调试步骤

### 第一步：添加调试组件
1. 选择Enemy GameObject
2. 添加 **EnemyDebugger** 组件
3. 勾选 "Show Debug Info" 和 "Show Gizmos In Game"

### 第二步：查看控制台输出
运行游戏，查看Console面板中的调试信息：
- "Enemy found player: Player" ✓
- "Distance to player: X.XX, Detection range: X.XX"
- "Enemy moving towards player"
- "Enemy moving with velocity: (X.XX, X.XX)"

### 第三步：检查Scene视图
在Scene视图中应该看到：
- 黄色圆圈：检测范围
- 红色圆圈：攻击范围  
- 绿色线条：Enemy到Player的连线

## 快速修复清单

□ Player GameObject有"Player"标签
□ Enemy有Rigidbody2D组件
□ Enemy有Collider2D组件  
□ Enemy有EnemyAI脚本
□ Enemy有HealthSystem脚本
□ Rigidbody2D的Body Type设为Dynamic
□ Rigidbody2D的Freeze Rotation Z已勾选
□ detectionRange值足够大(建议5-10)
□ moveSpeed值大于0(建议2-5)
□ Enemy和Player距离在检测范围内

## 如果仍然不工作

1. **暂时移除HealthSystem检查**
   在EnemyAI.cs的Update方法中注释掉：
   ```csharp
   // if (healthSystem != null && !healthSystem.IsAlive())
   //     return;
   ```

2. **简化移动逻辑**
   在EnemyAI.cs中添加强制移动测试：
   ```csharp
   // 在Update()最后添加
   if (Input.GetKey(KeyCode.T)) // 按T键测试移动
   {
       rb.velocity = new Vector2(2f, rb.velocity.y);
   }
   ```

3. **检查物理设置**
   - Window → Analysis → Physics Debugger
   - 确保没有约束阻止移动

## 创建测试Enemy的快速方法

1. 创建空GameObject命名为"TestEnemy"
2. 添加组件：
   - Rigidbody2D
   - BoxCollider2D  
   - EnemyAI
   - HealthSystem
   - EnemyDebugger
3. 设置Rigidbody2D为Dynamic
4. 勾选Freeze Rotation Z
5. 运行测试