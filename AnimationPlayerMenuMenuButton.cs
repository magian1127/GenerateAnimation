#if TOOLS
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animation = Godot.Animation;
using Array = Godot.Collections.Array;

namespace Addons.GenerateAnimation;

/// <summary>
/// 生成AnimationPlayer指定动画用
/// </summary>
[Tool]
public partial class AnimationPlayerMenuMenuButton : MenuButton
{
    public Sprite2D? Sprite2D { get; set; }

    public AnimationPlayer? AnimationPlayer { get; set; }

    public override void _Ready()
    {
        base._Ready();
        //下拉菜单的按钮事件
        this.GetPopup().Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.GenerateAnimationPlayer_IdPressed)); ;
    }

    private void GenerateAnimationPlayer_IdPressed(long id)
    {
        Generate(Convert.ToBoolean(id));
    }

    private void Generate(bool reGenerate)
    {
        if (Sprite2D is null)
        {
            GD.Print($"未设置Sprite");
            return;
        }
        if (AnimationPlayer is null)
        {
            GD.Print($"未设置AnimationPlayer");
            return;
        }

        List<TrackData> tracks = new();
        using FileAccess trackFile = FileAccess.Open(this.SceneFilePath.Replace("AnimationPlayerMenuMenuButton.tscn", "DefaultTracks.txt"), FileAccess.ModeFlags.Read);
        while (!trackFile.EofReached())
        {
            string[] param = trackFile.GetCsvLine();
            if (param.Length > 2 && param[0] == "Track" && Enum.TryParse(typeof(Animation.TrackType), param[1], out object? s2) && s2 is Animation.TrackType trackType && !string.IsNullOrWhiteSpace(param[2]))
            {//查找Track开头的行
                TrackData trackData = new TrackData()
                {
                    TrackType = trackType,
                    Path = param[2]
                };
                if (param.Length > 3 && Enum.TryParse(typeof(Animation.UpdateMode), param[3], out object? s3) && s3 is Animation.UpdateMode updateMode)
                {
                    trackData.UpdateMode = updateMode;
                }

                param = trackFile.GetCsvLine();
                if (param.Length > 0 && param[0] == "AnimName")
                {//查找AnimName开头的行
                    for (int i = 1; i < param.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(param[i]))
                        {
                            trackData.AnimNames.Add(param[i]);
                        }
                    }

                    param = trackFile.GetCsvLine();
                    if (param.Length > 1 && param[0] == "Key")
                    {//查找Key开头的行

                        switch (trackData.TrackType)
                        {
                            case Animation.TrackType.Value:
                                for (int i = 1; i < param.Length; i++)
                                {
                                    var keyV = param[i].Split(":");
                                    if (keyV.Length > 1 && float.TryParse(keyV[0], out float key))
                                    {
                                        trackData.Keys.Add(key, new Array<Variant>() { CSVStringToVariant(keyV[1]) });
                                    }
                                }
                                break;
                            case Animation.TrackType.Method:
                                for (int i = 1; i < param.Length; i++)
                                {
                                    var keyV = param[i].Split(":");
                                    if (keyV.Length > 1 && float.TryParse(keyV[0], out float key))
                                    {// 帧时间:方法名:参数1:参数N
                                        var methodV = new Array<Variant>() { keyV[1] };
                                        if (keyV.Length > 2)
                                        {
                                            methodV.AddRange(keyV.Skip(2).Select(v => CSVStringToVariant(v)));
                                        }
                                        trackData.Keys.Add(key, methodV);
                                    }
                                }
                                break;
                            default:
                                break;
                        }

                        if (trackData.Keys.Count > 0)
                        {
                            tracks.Add(trackData);
                        }
                    }
                }
            }
        }

        using FileAccess file = FileAccess.Open(Sprite2D.Texture.ResourcePath.Replace(".png", ".txt"), FileAccess.ModeFlags.Read);
        string[] imgHW = file.GetCsvLine();//第一行记录的是长宽(例 128,96)
        if (imgHW.Length >= 2)
        {
            if (int.TryParse(imgHW[0], out int width))
            {//算出列数
                Sprite2D.Hframes = Sprite2D.Texture.GetWidth() / width;
            }
            else
            {
                GD.Print($"未正确设置动画宽度");
                return;
            }
            if (int.TryParse(imgHW[1], out int height))
            {//算出行数
                Sprite2D.Vframes = Sprite2D.Texture.GetHeight() / height;
            }
            else
            {
                GD.Print($"未正确设置动画高度");
                return;
            }
            GD.Print($"动画 {Sprite2D.Texture.GetWidth()}*{Sprite2D.Texture.GetHeight()} {width}*{height} {Sprite2D.Vframes}行{Sprite2D.Hframes}列");
        }
        file.GetLine();//跳过第二行,是标题
        int faram = 0;
        var fileIndex = 2;
        while (!file.EofReached())
        {
            string[] param = file.GetCsvLine();
            if (param.Length >= 2)
            {
                //faramNum 动作的帧数
                if (!int.TryParse(param[1].Trim(), out int faramNum) || faramNum < 1)
                {
                    GD.Print($"文本第 {fileIndex} 行帧数设置不正确");
                    break;
                }

                var animName = param[0].Trim();
                if (animName![0] == '_')
                {
                    GD.Print($"忽略 {animName}");
                }
                else if (AnimationPlayer.HasAnimation(animName))
                {
                    var anim = AnimationPlayer.GetAnimation(animName);

                    if (reGenerate)
                    {
                        int c = anim.GetTrackCount();
                        for (int i = 0; i < c; i++)
                        {
                            anim.RemoveTrack(0);
                        }
                    }

                    float secondFaram = 8;//每秒帧数
                    if (param.Length >= 3)
                    {
                        _ = float.TryParse(param[2].Trim(), out secondFaram);
                    }
                    anim.Step = 1f / secondFaram;//算出每帧的时间(0.X秒)
                    anim.Length = anim.Step * faramNum;//每帧时长乘以总帧数,获得动画时长

                    StringBuilder sb = new();

                    NodePath trackPath = $"{nameof(Sprite2D)}:{Sprite2D.PropertyName.Frame}";
                    if (anim.FindTrack(trackPath, Animation.TrackType.Value) == -1)
                    {
                        var idx = anim.AddTrack(Animation.TrackType.Value);
                        anim.TrackSetPath(idx, $"{nameof(Sprite2D)}:{Sprite2D.PropertyName.Frame}");
                        anim.ValueTrackSetUpdateMode(idx, Animation.UpdateMode.Discrete);

                        for (int i = 0; i < faramNum; i++)
                        {
                            anim.TrackInsertKey(idx, i * anim.Step, faram + i);
                        }
                        sb.Append($"轨道{trackPath}已设置 ");
                    }


                    foreach (var trackData in tracks.Where(t => t.AnimNames.Count == 0 || t.AnimNames.Contains(animName)))
                    {//插入默认配置中需要的轨道
                        AddTrack(trackData.Path.Replace("{animName}", animName), anim, trackData, sb);
                    }

                    if (sb.Length > 0)
                    {
                        GD.Print($"动作 {animName} {(reGenerate ? "重新" : "")}设置完成 ({sb})");
                    }
                    else
                    {
                        GD.Print($"动作 {animName} 已被设置过,忽略");
                    }
                }
                else
                {
                    GD.Print($"动作 {animName} 找不到");
                }

                faram += faramNum;
                if (param.Length >= 4 && int.TryParse(param[3], out int 换行) && 换行 == 1 && faram % Sprite2D.Hframes > 0)
                {//跳过剩余空白帧位置
                    faram += Sprite2D.Hframes - (faram % Sprite2D.Hframes);
                }
            }
            fileIndex++;
        }
    }

    class TrackData
    {
        public Animation.TrackType TrackType { get; set; }
        public string Path { get; set; } = null!;
        public Animation.UpdateMode? UpdateMode { get; set; }
        public Array<string> AnimNames { get; set; } = new();
        public Godot.Collections.Dictionary<float, Array<Variant>> Keys { get; set; } = new();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="trackPath">轨道路径</param>
    /// <param name="anim">AnimationPlayer中已经存在的动画</param>
    /// <param name="trackData">默认需要添加的轨道数据</param>
    /// <param name="sb">输出用的动态字符串</param>
    private static void AddTrack(NodePath trackPath, Animation anim, TrackData trackData, StringBuilder sb)
    {
        if (anim.FindTrack(trackPath, trackData.TrackType) == -1)
        {
            var idx = anim.AddTrack(trackData.TrackType);
            anim.TrackSetPath(idx, trackPath);
            switch (trackData.TrackType)
            {
                case Animation.TrackType.Value:
                    foreach (var keyV in trackData.Keys)
                    {
                        anim.TrackInsertKey(idx, keyV.Key == -1 ? anim.Length : keyV.Key, keyV.Value.First());//-1表示最后一帧
                    }
                    break;
                case Animation.TrackType.Method:
                    foreach (var keyV in trackData.Keys)
                    {
                        anim.TrackInsertKey(idx, keyV.Key == -1 ? anim.Length : keyV.Key, new Godot.Collections.Dictionary<string, Variant>() { { "method", keyV.Value.First() }, { "args", new Array(keyV.Value.Skip(1)) } });
                    }
                    break;
                case Animation.TrackType.Position3D:
                case Animation.TrackType.Rotation3D:
                case Animation.TrackType.Scale3D:
                case Animation.TrackType.BlendShape:
                case Animation.TrackType.Bezier:
                case Animation.TrackType.Audio:
                case Animation.TrackType.Animation:
                default:
                    break;
            }
            if (trackData.UpdateMode != null)
            {
                anim.ValueTrackSetUpdateMode(idx, trackData.UpdateMode.Value);
            }

            sb.Append($"轨道 {trackPath} - {trackData.TrackType.ToString()} 已设置 ");
        }
    }

    /// <summary>
    /// 目前只支持 bool double string .
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static Variant CSVStringToVariant(string s)
    {
        if (double.TryParse(s, out double dv))
        {
            return dv;
        }
        else if (bool.TryParse(s, out bool bv))
        {
            return bv;
        }
        else if (s.StartsWith('"'))
        {
            return s.Trim('"');
        }
        return new Variant();
    }

}
#endif