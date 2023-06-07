using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


    [CreateAssetMenu(fileName = "New LevelSettings Config", menuName = "LevelSettings Config")]
    public class MapLevelScriptable : ScriptableObject
    {
        public float duration;
        public List<Sprite> heightMapList;
        public AudioClip clip;
        
        
        public Color northColor;
        public Color eastColor;
        public Color southColor;
        public Color westColor;
        
        public Color acuteColorShader;
        public Color graveColorShader;
        
        [Range(0,1)] public float maxVolume;
        public float speedLerp;
        public float smoothDownRate;
        public float shapeMul;
        public float intencityMul;
        public float rangeMul;
        
    }

    public class MapSettings
    {
        private readonly List<NativeArray<float>> _arrayEntityList;
        
        public readonly float Duration;
        private int _currentIndex;
       
            

        public MapSettings(MapLevelScriptable mapLevel)
        {
            Duration = mapLevel.duration;

            _arrayEntityList = new List<NativeArray<float>>();
            for (var index = 0; index < mapLevel.heightMapList.Count; index++)
                _arrayEntityList.Add(GenerateTextureArray(mapLevel.heightMapList[index], index));

         
        }

        private NativeArray<float> GenerateTextureArray(Sprite texture2D, int index)
        {
        
            var array = new NativeArray<float>(64 * 64, Allocator.Persistent);

            var offset = GetGilada(index) * 64;
            
            for (var i = 0; i < 64; i++)
            for (var j = 0; j < 64; j++)
                array[i * 64 + j] = texture2D.texture.GetPixel(offset.x + i, offset.y + j).r;

            return array;
        }

        int2 GetGilada(int index)
        {
            var y = index / 4;
            var x = index % 4;
            return new int2(x,y);
        }
        public void OnDisable()
        {
            for (var i = 0; i < _arrayEntityList.Count; i++)
            {
                if (_arrayEntityList[i].IsCreated)
                    _arrayEntityList[i].Dispose();
            }
        }


        public NativeArray<float> GetCurrentLevel() => _arrayEntityList[_currentIndex];
        public NativeArray<float> GetNextLevel()
        {
            _currentIndex ++;
            if (_currentIndex >= _arrayEntityList.Count)
                _currentIndex = 0;
            return _arrayEntityList[_currentIndex];
        }

        

    }
