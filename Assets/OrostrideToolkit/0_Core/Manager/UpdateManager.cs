using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrostrideToolkit.Core
{
    public interface IUpdatable { void OnUpdate(float dt); }
    public interface IFixedUpdatable { void OnFixedUpdate(float fdt); }
    public interface ILateUpdatable { void OnLateUpdate(float dt); }

    [DefaultExecutionOrder(-1000)]
    public class UpdateManager : Singleton<UpdateManager>
    {
        private class UpdateBatch<T> where T : class
        {
            public readonly List<T> ActiveList = new(128);
            
            private readonly HashSet<T> _lookUp = new();
            private readonly List<T> _toAdd = new();
            private readonly List<T> _toRemove = new();
            private readonly object _lock = new();

            public void Register(T item)
            {
                if (item == null) return;

                lock (_lock)
                {
                    if (_lookUp.Add(item))
                    {
                        // Re-registered before a pending removal was applied: it is still in ActiveList,
                        // so cancelling the removal is enough. Adding it again would update it twice per frame.
                        if (!_toRemove.Remove(item)) _toAdd.Add(item);
                    }
                }
            }

            public void Unregister(T item)
            {
                if (item == null) return;

                lock (_lock)
                {
                    if (_lookUp.Remove(item))
                    {
                        // Unregistered before a pending add was applied: it never reached ActiveList.
                        if (!_toAdd.Remove(item)) _toRemove.Add(item);
                    }
                }
            }

            public void ApplyChanges()
            {
                lock (_lock)
                {
                    if (_toRemove.Count > 0)
                    {
                        for (int i = 0; i < _toRemove.Count; i++)
                        {
                            T item = _toRemove[i];
                            int index = ActiveList.IndexOf(item);
                            
                            if (index != -1)
                            {
                                int lastIndex = ActiveList.Count - 1;
                                ActiveList[index] = ActiveList[lastIndex];
                                ActiveList.RemoveAt(lastIndex);
                            }
                        }
                        _toRemove.Clear();
                    }

                    if (_toAdd.Count > 0)
                    {
                        ActiveList.AddRange(_toAdd);
                        _toAdd.Clear();
                    }
                }
            }
        }

        private readonly UpdateBatch<IUpdatable> _updateBatch = new();
        private readonly UpdateBatch<IFixedUpdatable> _fixedBatch = new();
        private readonly UpdateBatch<ILateUpdatable> _lateBatch = new();

        protected override void Awake()
        {
            singletonType = SingletonType.Global;
            base.Awake();
        }
        
        public static bool IsPaused { get; set; }

        public static void Register(object obj)
        {
            if (obj == null || Instance == null) return;
            if (obj is IUpdatable u) Instance._updateBatch.Register(u);
            if (obj is IFixedUpdatable fu) Instance._fixedBatch.Register(fu);
            if (obj is ILateUpdatable lu) Instance._lateBatch.Register(lu);
        }

        public static void Unregister(object obj)
        {
            if (obj == null || Instance == null) return;
            if (obj is IUpdatable u) Instance._updateBatch.Unregister(u);
            if (obj is IFixedUpdatable fu) Instance._fixedBatch.Unregister(fu);
            if (obj is ILateUpdatable lu) Instance._lateBatch.Unregister(lu);
        }

        private void Update()
        {
            if (IsPaused) return;
            _updateBatch.ApplyChanges();
            
            float dt = Time.deltaTime;
            var list = _updateBatch.ActiveList;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try
                {
                    list[i].OnUpdate(dt);
                }
                catch (Exception ex)
                {
                    _updateBatch.Unregister(list[i]);
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsPaused) return;
            _fixedBatch.ApplyChanges();

            float fdt = Time.fixedDeltaTime;
            var list = _fixedBatch.ActiveList;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try
                {
                    list[i].OnFixedUpdate(fdt);
                }
                catch (Exception ex)
                {
                    _fixedBatch.Unregister(list[i]);
                }
            }
        }

        private void LateUpdate()
        {
            if (IsPaused) return;
            _lateBatch.ApplyChanges();

            float dt = Time.deltaTime;
            var list = _lateBatch.ActiveList;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try
                {
                    list[i].OnLateUpdate(dt);
                }
                catch (Exception ex)
                {
                    _lateBatch.Unregister(list[i]);
                }
            }
        }
    }
}