using Godot;
using System;
using System.Collections;

public partial class RowGeneration : Node2D
{
    public const int ROW_SIZE = 16;
    public const int ROW_HEIGHT = 16;

    /* 用于生成 Row 的布尔值列表
     */
    public BitArray rowList = null;

    /* 用于返回第 x 个元素是否为真
     */
    public bool IsTrueOnRow(int x)
    {
        bool inBounds = x >= 0 && x < rowList.Length;
        bool isTrue = inBounds && rowList[x];
        return isTrue;
    }
    public bool IsTrueOnRow(double x)
    {
        return IsTrueOnRow((int)Math.Floor(x));
    }
    /* 用于返回从 x 到 y 之间的真值的数量
     */
    public double IsTrueOnRow(double x, double y)
    {
        if (x>y) return 0;

        x = Math.Clamp(x, 0, rowList.Length);
        y = Math.Clamp(y, 0, rowList.Length);
        int xFloor = Math.Clamp((int)Math.Floor(x), 0, rowList.Length);
        int yCeil = Math.Clamp((int)Math.Ceiling(y), 0, rowList.Length);

        if (xFloor == yCeil) return rowList[xFloor] ? y - x : 0;

        double sum = 0;
        if (rowList[xFloor]) sum += xFloor + 1 - x;
        if (rowList[yCeil]) sum += y - yCeil;
        for (int i = xFloor + 1; i < yCeil; i++)
            if (rowList[i]) sum++;

        return sum;
    }

    /* 用于生成 Row 的 PackScene */
    [Export]
    public PackedScene left;
    [Export]
    public PackedScene right;
    [Export]
    public PackedScene middle;
    [Export]
    public PackedScene single;

    private PackedScene GetScene(bool leftElement, bool element, bool rightElement) => (leftElement, element, rightElement) switch
    {
        (var l, false, var r) => null,
        (false, true, false) => single,
        (false, true, true) => left,
        (true, true, false) => right,
        (true, true, true) => middle,
    };
    /*
     * 这个函数会在每次调用时，清空所有子节点，然后根据rowList生成新的子节点
     * 会在_Ready()中调用一次
     */
    public void PrepareRow()
    {
        foreach (Node2D child in GetChildren())
        {
            child.QueueFree();
        }
        if (rowList == null) return;
        for (int i = 0; i < rowList.Length; i++)
        {
            bool leftElement = i == 0 ? false : rowList[i - 1];
            bool element = rowList[i];
            bool rightElement = i == rowList.Length - 1 ? false : rowList[i + 1];
            PackedScene scene = GetScene(leftElement, element, rightElement);
            if (scene != null)
            {
                Node2D cell = scene.Instantiate<Node2D>();
                cell.Position = Vector2.Right * i * ROW_SIZE;
                AddChild(cell);

            }
        }
    }
    /* 用于设置并准备 Row，见PrePareRow()
     */
    public void SetAndPrepareRow(BitArray rowList)
    {
        this.rowList = rowList;
        PrepareRow();
    }
}
