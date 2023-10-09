namespace SimplestarGame
{
    public struct XYZ
    {
        public byte x;
        public byte y;
        public byte z;

        public static XYZ operator *(XYZ xyz, int multiplier)
        {
            XYZ result;
            result.x = (byte)(xyz.x * multiplier);
            result.y = (byte)(xyz.y * multiplier);
            result.z = (byte)(xyz.z * multiplier);
            return result;
        }
    }
}
