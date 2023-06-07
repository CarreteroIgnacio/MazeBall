
namespace Unity.Collections
{
    public static class NativeListExt
    {

        
        /// <summary>
        /// Removes the order of the element in the NativeList.
        /// </summary>
        public static void ReverseList<T>(ref this NativeList<T> nativeList) where T : unmanaged
        {
            var copy = nativeList;
            for (var i = 0; i < nativeList.Length; i++) 
                nativeList[i] = copy[nativeList.Length-1 - i];
            copy.Dispose();
        }
        
        
        public static NativeList<T> Trim<T>(this NativeList<T> nativeArray, int trimLenght, Allocator allocator) where T : unmanaged
        {
            var list = new NativeList<T>( allocator);

            for (var i = 0; i < trimLenght; i++) 
                list.Add(i < nativeArray.Length ? nativeArray[i] : new T());

            return list;
        }
        
        public static void Trim2<T>(this ref NativeList<T> nativeArray, int trimLenght, Allocator allocator) where T : unmanaged
        {
            var list = new NativeList<T>( allocator);

            for (var i = 0; i < trimLenght; i++) 
                list.Add(i < nativeArray.Length ? nativeArray[i] : new T());

            nativeArray.Dispose();
            nativeArray = list;
            list.Dispose();
        }



        
        
  










    }
}