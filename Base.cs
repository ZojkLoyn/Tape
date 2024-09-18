using Godot;
using System;
using System.Collections;

abstract class RowMoving
{
    protected Base parent;
    public RowMoving(Base parent, string rowName, int BaseY)
    {
        this.parent = parent;
        Init(0, rowName, BaseY);
    }
    public Vector2 BasePosition = Vector2.Zero;
    public string rowName;
    public RowGeneration row {
        get => parent.GetNode<RowGeneration>(rowName);
    }
#nullable enable
    virtual public void Init(double note16Offset = 0, string? rowName = null, int? BaseY = null)
#nullable disable
    {
        if (rowName != null) this.rowName = rowName;
        if (BaseY != null) BasePosition = Vector2.Down * (int)BaseY * RowGeneration.ROW_HEIGHT;
        note16Time = note16Offset;
    }

    protected abstract int direction2 {
        get;
    }
    public double note16Time = 0;
    protected void UpdateNote16Time(double note16Delta)
    {
        note16Time += note16Delta * direction2 / 2;
    }
    virtual public void Process(double note16Delta)
    {
        UpdateNote16Time(note16Delta);
        row.Position = Vector2.Left * (float)note16Time * RowGeneration.ROW_SIZE + BasePosition;
    }

    public bool IsTrueOnRow() => row.IsTrueOnRow(note16Time);
}
class ObjectRowMoving : RowMoving
{
    public ObjectRowMoving(Base parent, string rowName, int BaseY) : base(parent, rowName, BaseY) { }
    protected override int direction2 {
        get => 2;
    }
}
class PatternRowMoving : RowMoving
{
    public PatternRowMoving(Base parent, string rowName, int BaseY) : base(parent, rowName, BaseY) { }
    public PatternSpeedStateMachine SpeedState = new PatternSpeedStateMachine();
    public class PatternSpeedStateMachine{
        private PatternSpeedState _SpeedState;
        public enum PatternSpeedState
        {
            Forward = 2,
            FastForward = 4,
            SlowForward = 1,
        }
        public int val {
            get => (int)_SpeedState;
        }
        public void Fast() {
            _SpeedState = PatternSpeedState.FastForward;
        }
        public void Slow() {
            _SpeedState = PatternSpeedState.SlowForward;
        }
        public void Normal() {
            _SpeedState = PatternSpeedState.Forward;
        }
    }
    public PatternDirectionStateMachine DirectionState = new PatternDirectionStateMachine();
    public class PatternDirectionStateMachine
    {
        private PatternDirectionState _DirectionState;
        public enum PatternDirectionState
        {
            Left = 1,
            Right = -1,
        }
        public int val {
            get => (int)_DirectionState;
        }
        public void Turn() {
            _DirectionState = (PatternDirectionState)(-(int)_DirectionState);
        }
        public void Left() {
            _DirectionState = PatternDirectionState.Left;
        }
        public void Right() {
            _DirectionState = PatternDirectionState.Right;
        }
    }
    protected override int direction2 {
        get => DirectionState.val * SpeedState.val;
    }
#nullable enable
    public override void Init(double note16Offset = 0, string? rowName = null, int? BaseY = null)
#nullable disable
    {
        base.Init(note16Offset, rowName, BaseY);
        SpeedState.Normal();
        DirectionState.Left();
    }
}
class InputHandler {
    private bool _IsSlowPressed = false;
    private bool _IsFastPressed = false;
    private PatternRowMoving _PatternRowMoving;
    public InputHandler(PatternRowMoving patternRowMoving)
    {
        _PatternRowMoving = patternRowMoving;
    }
    public void TurnDirection()
    {
        _PatternRowMoving.DirectionState.Turn();
    }
    public void SpeedUpdate() {
        if (_IsFastPressed) {
            _PatternRowMoving.SpeedState.Fast();
        } else if (_IsSlowPressed) {
            _PatternRowMoving.SpeedState.Slow();
        } else {
            _PatternRowMoving.SpeedState.Normal();
        }
    }
    public void SlowPressed()
    {
        _IsSlowPressed = true;
        SpeedUpdate();
    }
    public void SlowReleased()
    {
        _IsSlowPressed = false;
        SpeedUpdate();
    }
    public void FastPressed()
    {
        _IsFastPressed = true;
        SpeedUpdate();
    }
    public void FastReleased()
    {
        _IsFastPressed = false;
        SpeedUpdate();
    }
}
class ScoreCalc
{
    public double score {
        get {
            UpdateHitState();
            return (hit + extendHit) / maxHit;
        }
    }

    public double hit = 0;
    public double extendHit {
        get => _HitState ? _OnHit : 0;
    }
    private bool _HitState = false;
    private double _OnHitNote16Time = 0;
    private double _OnHit {
        get => objectRow.row.IsTrueOnRow(_OnHitNote16Time, objectRow.note16Time);
    }
    private void UpdateHitState()
    {
        if (patternRow.IsTrueOnRow() != _HitState) {
            if (_HitState) {
                hit += _OnHit;
                _HitState = false;
            } else {
                _OnHitNote16Time = objectRow.note16Time;
                _HitState = true;
            }
        }
    }
    public double maxHit = 0;

    public ObjectRowMoving objectRow;
    public PatternRowMoving patternRow;
    public void Ready()
    {
        hit = 0;
        maxHit = 0;
        _HitState = false;
        _OnHitNote16Time = 0;
        foreach (bool rowElement in objectRow.row.rowList) {
            if (rowElement) maxHit++;
        }
    }
    public ScoreCalc(ObjectRowMoving objectRow, PatternRowMoving patternRow)
    {
        this.objectRow = objectRow;
        this.patternRow = patternRow;
    }
}
public partial class Base : Node2D
{
    private ObjectRowMoving _ObjectRowMoving;
    private PatternRowMoving _PatternRowMoving;
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("turn_direction")) {
            _InputHandler.TurnDirection();
        }
        if (@event.IsActionPressed("slow_forward")) {
            _InputHandler.SlowPressed();
        }
        if (@event.IsActionReleased("slow_forward")) {
            _InputHandler.SlowReleased();
        }
        if (@event.IsActionPressed("fast_forward")) {
            _InputHandler.FastPressed();
        }
        if (@event.IsActionReleased("fast_forward")) {
            _InputHandler.FastReleased();
        }
    }

    private ScoreCalc _ScoreCalc;
    public double score {
        get => _ScoreCalc.score;
    }

    private InputHandler _InputHandler;

    public override void _PhysicsProcess(double delta)
    {
        _ObjectRowMoving.Process(delta * _n16ps);
        _PatternRowMoving.Process(delta * _n16ps);
    }
    public override void _Ready()
    {
        _ObjectRowMoving = new ObjectRowMoving(this, "ObjectRow", 1);
        _PatternRowMoving = new PatternRowMoving(this, "PatternRow", 3);
        _ScoreCalc = new ScoreCalc(_ObjectRowMoving, _PatternRowMoving);
        _InputHandler = new InputHandler(_PatternRowMoving);
    }
    public void InitReady(BitArray objectRow, BitArray patternRow, double npm, double offset=-3*16)
    {
        UpdateObjectRow(objectRow);
        UpdatePatternRow(patternRow);
        _ScoreCalc.Ready();
        this.npm = npm;
        _ObjectRowMoving.Init(offset);
    }
    public void UpdateObjectRow(BitArray rowList)
    {
        _ObjectRowMoving.row.SetAndPrepareRow(rowList);
    }
    public void UpdatePatternRow(BitArray rowList)
    {
        _PatternRowMoving.row.SetAndPrepareRow(rowList);
    }

    private double _n16ps = 1;
    [Export]
    public double npm {
        get => _n16ps / (60 * 16);
        set => _n16ps = value * 60 * 16;
    }
}
