namespace RealEstateDemo.Core
{
    // XorShift32：随机状态直接存入存档，读档不会重抽另一组事件。
    public static class GameRandom
    {
        public static uint NextUInt(GameState state)
        {
            uint x = state.RngState == 0 ? 0x9E3779B9u : state.RngState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state.RngState = x; return x;
        }
        public static int Range(GameState state, int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextUInt(state) % (uint)(maxExclusive - min));
        }
        public static float Float(GameState state) { return NextUInt(state) / 4294967295f; }
    }
}
