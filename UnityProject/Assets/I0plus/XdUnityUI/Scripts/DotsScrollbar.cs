﻿/**
 * @author Kazuma Kuwabara
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Baum2
{
    [RequireComponent(typeof(ToggleGroup))]
    public sealed class DotsScrollbar : Scrollbar
    {
        [SerializeField] private Transform dotContainer;

        [SerializeField] private Toggle dotPrefab;

        [SerializeField] private List<Toggle> dots = null;

        private ToggleGroup dotGroup;

        public bool IsValid
        {
            get { return dotContainer != null && dotPrefab != null; }
        }

        public Transform DotContainer
        {
            set { dotContainer = value; }
            get { return dotContainer; }
        }

        public Toggle DotPrefab
        {
            set { dotPrefab = value; }
            get { return dotPrefab; }
        }

        protected override void Start()
        {
            base.Start();
            Setup();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (UnityEngine.Application.isPlaying)
            {
                onValueChanged.AddListener(OnScrollValueChanged);
                foreach (var toggle in dots)
                    toggle.onValueChanged.AddListener(OnToggleValueChange);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (UnityEngine.Application.isPlaying)
            {
                onValueChanged.RemoveListener(OnScrollValueChanged);
                foreach (var toggle in dots)
                    toggle.onValueChanged.RemoveListener(OnToggleValueChange);
            }
        }

        private void FixedUpdate()
        {
            if (IsValid)
                UpdateDots();
        }

        private void Setup()
        {
            if (Application.isPlaying && dotPrefab != null)
            {
                dotPrefab.gameObject.SetActive(false);
            }

            numberOfSteps = 0;
            SetupDotContainer();
            SetupDotGroup();
            SetupHandleRect();

            /*
            if (!IsValid)
                Debug.unityLogger.LogWarning("DotsScrollbar", "Invalid Serialize Field");
                */
        }

        private void SetupDotContainer()
        {
            if (dotContainer == null && dotPrefab != null)
            {
                dotContainer = dotPrefab.transform.parent;
            }
        }

        private void SetupDotGroup()
        {
            if (dotGroup == null)
                dotGroup = GetComponent<ToggleGroup>();
            if (dotGroup == null)
                dotGroup = gameObject.AddComponent<ToggleGroup>();
            dotGroup.allowSwitchOff = false;
        }

        private void SetupHandleRect()
        {
            if (handleRect != null)
                return;

            if (Application.isPlaying)
            {
                // handleがないとScrollbarが動作しないのでダミーを設定する
                var dummyHandle = new GameObject("Dummy Handle").AddComponent<LayoutElement>();
                dummyHandle.transform.SetParent(dotContainer);
                dummyHandle.ignoreLayout = true;
                handleRect = dummyHandle.GetComponent<RectTransform>();
            }
        }

        private void UpdateDots()
        {
            if (!Application.isPlaying) return;
            var newDotCount = Mathf.CeilToInt(1.0f / size);
            if (newDotCount != dots.Count)
            {
                var count = newDotCount - dots.Count;
                if (count > 0)
                    AddDots(count);
                else if (count < 0)
                    RemoveDots(-count);

                OnScrollValueChanged(value);
            }
        }

        private void AddDots(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var dot = Instantiate(dotPrefab, dotContainer);
                dot.gameObject.SetActive(true);
                dot.group = dotGroup;
                if (UnityEngine.Application.isPlaying)
                    dot.onValueChanged.AddListener(OnToggleValueChange);
                dots.Add(dot);
            }
        }

        private void RemoveDots(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (dots.Count <= 0)
                    return;

                var index = dots.Count - 1;
                var dot = dots[dots.Count - 1];
                if (UnityEngine.Application.isPlaying)
                    dot.onValueChanged.RemoveListener(OnToggleValueChange);
                DestroyImmediate(dot.gameObject);
                dots.RemoveAt(index);
            }
        }

        private float StepSize()
        {
            var ofSteps = dots.Count - 1;
            return (ofSteps > 1) ? 1f / ofSteps : 0.001f;
        }

        private void OnScrollValueChanged(float input)
        {
            var step = Mathf.RoundToInt(input / StepSize());
            for (var i = 0; i < dots.Count; i++)
            {
                var dot = dots[i];
#if Unity_2019_1_NEWER
                dot.SetIsOnWithoutNotify(i == step);
#else
                var isOn = i == step;
                if (dot.isOn != isOn)
                {
                    var tmp = dots[i].onValueChanged;
                    dot.onValueChanged = new Toggle.ToggleEvent();
                    dot.isOn = isOn;
                    dot.onValueChanged = tmp;
                }
#endif
            }
        }

        private bool scrolling = false;

        private IEnumerator ChangeValue(float targetValue)
        {
            scrolling = true;
            var nowValue = value;
            for (float i = 1; i <= 10; i++)
            {
                value = nowValue + (targetValue - nowValue) * (i / 10);
                yield return null;
            }

            scrolling = false;
        }

        private void OnToggleValueChange(bool input)
        {
            if (scrolling) return;
            var step = dots.FindIndex(x => x.isOn);
            StartCoroutine(ChangeValue(step / (dots.Count - 1.0f)));
        }

#if UNITY_EDITOR

        public bool isAutoLayoutEnableOnEditMode = true;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (isAutoLayoutEnableOnEditMode)
                Setup();
        }

        protected override void Reset()
        {
            base.Reset();
            if (dots != null)
            {
                RemoveDots(dots.Count);
            }
        }

        public void ClearDotInstances()
        {
            if (dots != null)
            {
                RemoveDots(dots.Count);
            }
        }

        [UnityEditor.MenuItem("CONTEXT/DotScrollbar/Reset dot instances")]
        private static void ClearDotInstances(UnityEditor.MenuCommand menuCommand)
        {
            var self = menuCommand.context as DotsScrollbar;
            if (self != null)
                self.ClearDotInstances();
        }
#endif
    }
}