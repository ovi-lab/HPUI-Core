namespace HPUI.Utils
{
    public class Coord {
        private int _x, _y = 0;
        
	public int x{get {return _x;} set{StateChanged = true; _x = value;}}
	public int y{get {return _y;} set{StateChanged = true; _y = value;}}
	public float maxX=1;
	public float maxY=1;

        public bool StateChanged {get; private set;} = false;

        public void Reset()
        {
            x=0;
            y=0;
            StateChanged = false;
        }
    }
}
