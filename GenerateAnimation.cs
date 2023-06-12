#if TOOLS
using Godot;

namespace Addons.GenerateAnimation;

/// <summary>
/// 根据配置为AnimationPlayer和Sprite2D生成动画
/// </summary>
[Tool]
public partial class GenerateAnimation : EditorPlugin
{
    EditorSelection editorSelection = null!;
    AnimationPlayerMenuMenuButton? animationPlayerMenuMenuButton;

    public override void _EnterTree()
    {// 初始化插件
        editorSelection = GetEditorInterface().GetSelection();

        editorSelection.Connect(EditorSelection.SignalName.SelectionChanged, new Callable(this, GenerateAnimation.MethodName.OnSelectionChanged));

        animationPlayerMenuMenuButton = (AnimationPlayerMenuMenuButton)GD.Load<PackedScene>("addons/GenerateAnimation/AnimationPlayerMenuMenuButton.tscn").Instantiate();
        animationPlayerMenuMenuButton.Visible = false;
        AddControlToContainer(CustomControlContainer.CanvasEditorMenu, animationPlayerMenuMenuButton);
    }

    private void OnSelectionChanged()
    {
        // 获取当前选中的节点
        var selectedNodes = editorSelection!.GetSelectedNodes();
        if (selectedNodes.Count != 1)
        {
            return;
        }

        var selectedNode = selectedNodes[0];
        if (selectedNode is AnimationPlayer animationPlayer && animationPlayer.GetParent().FindChild("Sprite2D") is Sprite2D sprite2D)
        {
            animationPlayerMenuMenuButton!.Sprite2D = sprite2D;
            animationPlayerMenuMenuButton.AnimationPlayer = animationPlayer;
            animationPlayerMenuMenuButton.Visible = true;
        }
        else
        {
            animationPlayerMenuMenuButton!.Visible = false;
            animationPlayerMenuMenuButton.Sprite2D = null;
            animationPlayerMenuMenuButton.AnimationPlayer = null;
        }
    }

    public override void _ExitTree()
    {// 清理插件
        editorSelection!.Disconnect(EditorSelection.SignalName.SelectionChanged, new Callable(this, GenerateAnimation.MethodName.OnSelectionChanged));

        RemoveControlFromContainer(CustomControlContainer.CanvasEditorMenu, animationPlayerMenuMenuButton);
        animationPlayerMenuMenuButton!.Free();
    }
}
#endif
