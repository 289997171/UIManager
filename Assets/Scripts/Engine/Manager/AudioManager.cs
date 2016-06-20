using System.Collections.Generic;
using Engine.Common;
using Engine.Common.Singleton;
using UnityEngine;

namespace Engine.Manager
{
    /// <summary>
    /// 音乐管理器，在某个位置创建音乐，并自动播放
    /// </summary>
    public class AudioManager : DDOSingleton<AudioManager>, IManager
    {
        /// <summary>
        /// 循环播放的
        /// </summary>
        private List<AudioSource> loops;

        /// <summary>
        /// 只播放一次的
        /// </summary>
        private List<AudioSource> shots;

        public bool Init()
        {
            loops = new List<AudioSource>();
            shots = new List<AudioSource>();
            return true;
        }

        void OnEnable()
        {
            // 启用循环检测那些临时片段音乐播放完毕
            InvokeRepeating("CheckShots", 1f, 0.3f);
        }

        void OnDisable()
        {
            // 禁用循环检测那些临时片段音乐播放完毕
            CancelInvoke("CheckShots");
        }

        /// <summary>
        /// 循环检测那些片段播放完毕
        /// </summary>
        private void CheckShots()
        {
            Debug.Log("<< CheckShots !");

            AudioSource[] audioSources = shots.ToArray();

            for (int i = 0; i < audioSources.Length; i++)
            {
                AudioSource audioSource = audioSources[i];
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = null;
                    shots.Remove(audioSource);
                    Destroy(audioSource.gameObject);
                }
            }
        }


        /// <summary>
        /// 暂停所有的音乐
        /// </summary>
        public void PauseAll()
        {
            for (int i = 0; i < loops.Count; i++)
            {
                AudioSource audioSource = loops[i];
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Pause();    // 对于循环音乐，等时长较长的，使用暂停
            }

            for (int i = 0; i < shots.Count; i++)
            {
                AudioSource audioSource = shots[i];
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.volume = 0f;     // 对于片段音乐，等时长较短的，降低音量
            }
        }

        /// <summary>
        /// 暂停后重新播放
        /// </summary>
        public void ReplayAll()
        {
            for (int i = 0; i < loops.Count; i++)
            {
                AudioSource audioSource = loops[i];
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Play();    // 对于循环音乐，等时长较长的，重新播放
            }

            for (int i = 0; i < shots.Count; i++)
            {
                AudioSource audioSource = shots[i];
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.volume = 1f;     // 对于片段音乐，等时长较短的，恢复音量
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < loops.Count; i++)
            {
                AudioSource audioSource = loops[i];
                if (audioSource != null)
                    Destroy(audioSource.gameObject);
            }

            loops.Clear();

            for (int i = 0; i < shots.Count; i++)
            {
                AudioSource audioSource = shots[i];
                if (audioSource != null && audioSource.isPlaying)
                    Destroy(audioSource.gameObject);
            }

            shots.Clear();
        }

        public AudioSource Play(AudioClip clip, Transform emitter, bool loop = false, float volume = 1f, float pitch = 1f)
        {
            if (loop && loops.Count > 3)
            {
                Debug.LogError("动态创建的循环音效数量超过3了");
                return null;
            }

            if (!loop && shots.Count > 10)
            {
                Debug.LogError("动态创建的片段音效数量超过10了");
                return null;
            }

            // 创建空对象
            GameObject go = new GameObject("Audio: " + clip.name);
            go.transform.position = emitter.position;
            go.transform.parent = emitter;

            // 添加音源组件
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();

            // 播放完毕销毁
            if (loop) loops.Add(source);
            else shots.Add(source);

            return source;
        }

        public AudioSource Play(AudioClip clip, Vector3 point, bool loop = false, float volume = 1f, float pitch = 1f)
        {
            // 创建空对象
            GameObject go = new GameObject("Audio: " + clip.name);
            go.transform.position = point;

            // 添加音源组件
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();

            // 播放完毕销毁
            if (loop) loops.Add(source);
            else shots.Add(source);

            return source;
        }

    }
}