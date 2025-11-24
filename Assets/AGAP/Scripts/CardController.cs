using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AGAP
{
    public class CardController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private RectTransform _frontSide;
        [SerializeField] private RectTransform _backSide;
        [SerializeField] private Button _mainButton;
        [Header("Flip Settings")]
        [SerializeField] private float _flipDuration = 0.25f;
        #endregion

        #region Public Fields
        public bool IsFaceUp { get; private set; }
        #endregion
        
        #region Events and Delegates
        public event Action<CardController> Clicked;
        #endregion

        #region Private Fields
        private Tween _flipTween;
        #endregion

        #region Unity Functions
        private void Awake()
        {
            if (_mainButton == null)
                _mainButton = GetComponent<Button>();

            SetImmediateFaceUp(false);

            if (_mainButton != null)
                _mainButton.onClick.AddListener(OnCardClicked);
        }

        private void OnDestroy()
        {
            if (_mainButton != null)
                _mainButton.onClick.RemoveListener(OnCardClicked);

            _flipTween?.Kill();
        }
        #endregion

        #region Public Functions
       
        public void Flip(bool faceUp)
        {
            if (IsFaceUp == faceUp)
                return;

            IsFaceUp = faceUp;

            _flipTween?.Kill();

            RectTransform rect = (RectTransform)transform;

            _flipTween = rect.DOScaleX(0f, _flipDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    ApplyFaceVisibility(faceUp);

                    rect.localScale = new Vector3(0f, 1f, 1f);

                    _flipTween = rect.DOScaleX(1f, _flipDuration * 0.5f)
                        .SetEase(Ease.OutQuad);
                });
        }

        public void SetImmediateFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            _flipTween?.Kill();
            ApplyFaceVisibility(faceUp);

            RectTransform rect = (RectTransform)transform;
            rect.localScale = Vector3.one;
        }
        #endregion

        #region Private Functions

        private void OnCardClicked()
        {
            Clicked?.Invoke(this);
        }

        private void ApplyFaceVisibility(bool faceUp)
        {
            if (_frontSide != null)
                _frontSide.gameObject.SetActive(faceUp);

            if (_backSide != null)
                _backSide.gameObject.SetActive(!faceUp);
        }

        #endregion
    }
}
