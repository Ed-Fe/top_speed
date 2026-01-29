namespace TopSpeed.Tracks.Topology
{
    public enum ShapeType
    {
        Undefined = 0,
        Rectangle,
        Circle,
        Ring,
        Polygon,
        Polyline
    }

    public enum PortalRole
    {
        Undefined = 0,
        Entry,
        Exit,
        EntryExit
    }

    public enum LinkDirection
    {
        TwoWay = 0,
        OneWay
    }

}
