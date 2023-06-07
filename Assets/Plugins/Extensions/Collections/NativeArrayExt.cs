namespace Unity.Collections
{
    public static class NativeArrayExt
    {
        public static NativeArray<T> Trim<T>(this NativeArray<T> nativeArray, int trimLenght) where T : unmanaged
        {

            if (trimLenght < 1)
                trimLenght = 1;
            var nya = new NativeArray<T>(trimLenght, Allocator.Persistent);

            for (var i = 0; i < trimLenght; i++)
            {
                if (i >= nativeArray.Length - 1)
                    return nya;
                nya[i] = nativeArray[i];
            }

            return nya;
        }
        
        public static void ReverseArray<T>(ref this NativeArray<T> nativeArray) where T : unmanaged
        {
            var copy = nativeArray;
            for (var i = 0; i < nativeArray.Length; i++) 
                nativeArray[i] = copy[nativeArray.Length-1 - i];
        }
    }
}