namespace TopSpeed.Tracks.Walls
{
    public enum TrackWallMaterial
    {
        Undefined = 0,
        Hard,
        Soft,
        Rubber,
        Metal,
        Concrete,
        Wood,
        Dirt,
        Grass,
        Sand
    }

    public enum TrackWallCollisionMode
    {
        Block = 0,
        Bounce = 1,
        Pass = 2
    }
}
