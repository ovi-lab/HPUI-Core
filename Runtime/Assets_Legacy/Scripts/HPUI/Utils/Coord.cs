namespace ubco.ovilab.HPUI.Legacy.utils
{
    public class Coord {
        private int _x, _y = 0;
        
	public int x
        {
            get {
                return _x;
            }
            set {
                _x = value;
                OnStateChanged(_x, _y);
            }
        }
	public int y
        {
            get {
                return _y;
            }
            set {
                _y = value;
                OnStateChanged(_x, _y);
            }
        }

	public float maxX=1;
	public float maxY=1;
        public event System.Action<int, int> OnStateChanged;

        public void Reset()
        {
            SetCoord(0, 0);
        }

        public void SetCoord(int x, int y)
        {
            _x = x;
            _y = y;
            OnStateChanged?.Invoke(_x, _y);
        }
    }
}
