using Godot;
using System;
using System.Collections;

public partial class Game : Node2D
{
    public Base baseNode {
        get => GetNode<Base>("Base");
    }
    public Label scoreLabel {
        get => GetNode<Label>("CenterContainer/ScoreLabel");
    }

    public override void _PhysicsProcess(double delta)
    {
        scoreLabel.Text = string.Format("{0:P2}", baseNode.score);
    }
    public override void _Ready()
    {
        baseNode.InitReady(RandomRow(5), RandomRow(4),1.0/(4*60),-3);
    }
    private BitArray RandomRow(int byteLength)
    {
        var rowBytes = new uint[byteLength];
        for (int i = 0; i < byteLength; i++) {
            rowBytes[i] = GD.Randi();
        }
        byte[] bytes = new byte[byteLength * sizeof(uint)];
        Buffer.BlockCopy(rowBytes, 0, bytes, 0, bytes.Length);
        return new BitArray(bytes);
    }
    private BitArray OneRow(int byteLength) {
        var rowBytes = new int[byteLength];
        for (int i = 0; i < byteLength; i++) {
            rowBytes[i] = -1;
        }
        byte[] bytes = new byte[byteLength * sizeof(uint)];
        Buffer.BlockCopy(rowBytes, 0, bytes, 0, bytes.Length);
        return new BitArray(bytes);
    }
}
