# GenerateAnimation
Godot Addon Generate AnimationPlayer Animation

自用项目

节点的判断层级
```
CharacterBody2D
  -Sprite2D
  -AnimationTree (选择这里之后,编辑器上方会出现 生成动画 按钮)
```

生成动画根据 Sprite2D Texture 对应 png 同级目录下的 txt

目录示例
```
Assets/Sprite/Sprite.png
Assets/Sprite/Sprite.txt
```

动画txt配置示例,可复制下面的然后修改
```
288,128
名称(请按PNG上的顺序写),帧数,每秒帧数,下个动画换行开始(1是0否)
Idle,8,10,1
Run,10,10,1
_xx,1,10,1,_开头动画会被忽略
Jumping,3,10,1
Falling,3,10,1
_xx,1,10,1
Dodge,6,10,1
Attack,7,10,1
Attack2,21,10,1
UseSkill,12,10,1
Defend,12,10,1
TakeHit,7,10,1
Death,16,10,1
Transform,41,10,1
B/Idle,12,10,1,B是另外个动画库的名称
B/Run,6,10,1
B/Jumping,3,10,1
B/Falling,3,10,1
_e_air_atk,1,10,1
B/Attack,6,10,1
B/Attack2,12,10,1
B/TakeHit,6,10,1
```

其他属性和方法轨道请查看 插件目录下的 DefaultTracks.txt.
目前不支持单独设置,只支持在 DefaultTracks 中全局设置.
