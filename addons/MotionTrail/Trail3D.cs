[Tool, GlobalClass]
public partial class Trail3D : MeshInstance3D {
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool Paused { get; set; } = false;
    [Export] public float StartWidth { get; set; } = 0.5f;
    [Export] public float EndWidth { get; set; } = 0.0f;
    [Export(PropertyHint.Range, "0.5, 1.5")] public float ScaleAcceleration { get; set; } = 1.0f;
    [Export] public float MotionDelta { get; set; } = 0.1f;
    [Export] public double Lifetime { get; set; } = 1.0;
    [Export] public bool ScaleTexture { get; set; } = true;
    [Export] public Color StartColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    [Export] public Color EndColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    [Export] public InterpolationMode ColorInterpolationMode { get; set; } = InterpolationMode.Linear;
    [Export] public InterpolationDirection ColorInterpolationDirection { get; set; } = InterpolationDirection.Backward;

    private readonly List<TrailPoint> Points = [];
    private Vector3 OldPosition;

    public ImmediateMesh ImmediateMesh => (ImmediateMesh)Mesh;

    public override void _Ready() {
        OldPosition = GlobalPosition;
        Mesh ??= new ImmediateMesh();
    }
    public override void _Process(double Delta) {
        if (Paused) {
            return;
        }

        if (Enabled && GlobalPosition.DistanceTo(OldPosition) >= MotionDelta) {
            AddPoint();
            OldPosition = GlobalPosition;
        }

        for (int Index = 0; Index < Points.Count; Index++) {
            TrailPoint Point = Points[Index];

            Point.Lifetime += Delta;
            Points[Index] = Point;

            if (Point.Lifetime >= Lifetime) {
                RemovePoint(Index);
            }
        }

        Render();
    }
    public void Render() {
        ImmediateMesh.ClearSurfaces();

        if (Points.Count < 2) {
            return;
        }

        ImmediateMesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);

        for (int Index = 0; Index < Points.Count; Index++) {
            TrailPoint Point = Points[Index];

            float RawProgress = (float)Index / (Points.Count - 1);
            float Progress = ColorInterpolationDirection switch {
                InterpolationDirection.Forward => 1 - RawProgress,
                InterpolationDirection.Backward => RawProgress,
                _ => throw new NotImplementedException(ColorInterpolationDirection.ToString())
            };

            Color CurrentColor = ColorInterpolationMode switch {
                InterpolationMode.Linear => StartColor.Lerp(EndColor, Progress),
                InterpolationMode.Square => StartColor.Lerp(EndColor, float.Pow(Progress, 2)),
                InterpolationMode.Cube => StartColor.Lerp(EndColor, float.Pow(Progress, 3)),
                InterpolationMode.Quad => StartColor.Lerp(EndColor, float.Pow(Progress, 4)),
                InterpolationMode.Quint => StartColor.Lerp(EndColor, float.Pow(Progress, 5)),
                InterpolationMode.Sine => StartColor.Lerp(EndColor, float.Sin(Progress)),
                _ => throw new NotImplementedException(ColorInterpolationMode.ToString())
            };

            ImmediateMesh.SurfaceSetColor(CurrentColor);

            Vector3 CurrentWidth = Point.StartWidth - (Point.EndWidth * float.Pow(Progress, ScaleAcceleration));

            float T0, T1;
            if (ScaleTexture) {
                T0 = MotionDelta * Index;
                T1 = MotionDelta * (Index + 1);
            }
            else {
                T0 = Index / Points.Count;
                T1 = RawProgress;
            }
            ImmediateMesh.SurfaceSetUV(new Vector2(T0, 0));
            ImmediateMesh.SurfaceAddVertex(ToLocal(Point.Position + CurrentWidth));
            ImmediateMesh.SurfaceSetUV(new Vector2(T1, 1));
            ImmediateMesh.SurfaceAddVertex(ToLocal(Point.Position - CurrentWidth));
        }

        ImmediateMesh.SurfaceEnd();
    }
    public void AddPoint() {
        Vector3 Direction = OldPosition.DirectionTo(GlobalPosition);
        Rotation = Rotation with { Y = float.Atan2(Direction.X, Direction.Z) };

        Points.Add(new TrailPoint() {
            Position = GlobalPosition,
            StartWidth = GlobalBasis.X * StartWidth,
            EndWidth = (GlobalBasis.X * StartWidth) - (GlobalBasis.X * EndWidth),
        });
    }
    public void RemovePoint(int Index) {
        Points.RemoveAt(Index);
    }
    public void ClearPoints() {
        Points.Clear();
    }

    public enum InterpolationMode {
        Linear,
        Square,
        Cube,
        Quad,
        Quint,
        Sine,
    }
    public enum InterpolationDirection {
        Forward,
        Backward,
    }

    private struct TrailPoint() {
        public Vector3 Position { get; set; }
        public Vector3 StartWidth { get; set; }
        public Vector3 EndWidth { get; set; }
        public double Lifetime { get; set; } = 0.0;
    }
}