using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AGAP
{
    public class BoardController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Board Settings")]
        [SerializeField] private RectTransform _boardArea;
        [SerializeField] private GridLayoutGroup _gridLayout;
        [SerializeField] private CardController _cardPrefab;

        [Header("Layout")]
        [SerializeField] private int _rows = 2;
        [SerializeField] private int _columns = 2;
        [SerializeField] private Vector2 _spacing = new Vector2(10f, 10f);
        [SerializeField] private Vector2 _padding = new Vector2(20f, 20f);

        #endregion

        #region Public Fields

        public int Rows => _rows;
        public int Columns => _columns;
        public IReadOnlyList<CardController> Cards => _cards;

        #endregion

        #region Events and Delegates

        public event Action BoardBuilt;

        #endregion

        #region Private Fields

        private readonly List<CardController> _cards = new List<CardController>();

        #endregion

        #region Unity Functions

        private void Reset()
        {
            if (_boardArea == null)
                _boardArea = GetComponent<RectTransform>();

            if (_gridLayout == null)
                _gridLayout = GetComponent<GridLayoutGroup>();
        }

        private void Awake()
        {
            if (_boardArea == null)
                Debug.LogWarning("[BoardController] BoardArea is not assigned.", this);

            if (_gridLayout == null)
                Debug.LogWarning("[BoardController] GridLayoutGroup is not assigned.", this);

            if (_cardPrefab == null)
                Debug.LogWarning("[BoardController] Card prefab is not assigned.", this);
        }

        private void Start()
        {
            BuildBoard();
        }

        #endregion

        #region Public Functions

        public void BuildBoard()
        {
            if (_boardArea == null || _gridLayout == null || _cardPrefab == null)
            {
                Debug.LogError("[BoardController] Cannot build board – missing references.", this);
                return;
            }

            ClearExistingCards();
            ConfigureLayout();
            SpawnCards();

            BoardBuilt?.Invoke();
        }

        #endregion

        #region Private Functions

        private void ClearExistingCards()
        {
            _cards.Clear();

            for (int i = _boardArea.childCount - 1; i >= 0; i--)
            {
                var child = _boardArea.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        private void ConfigureLayout()
        {
            _gridLayout.spacing = _spacing;

            var rect = _boardArea.rect;

            var availableWidth = rect.width - (_padding.x * 2f);
            var availableHeight = rect.height - (_padding.y * 2f);

            if (_columns > 1)
                availableWidth -= _spacing.x * (_columns - 1);

            if (_rows > 1)
                availableHeight -= _spacing.y * (_rows - 1);

            var cellWidth = availableWidth / _columns;
            var cellHeight = availableHeight / _rows;

            var size = Mathf.Min(cellWidth, cellHeight);
            size = Mathf.Max(10f, size);

            _gridLayout.cellSize = new Vector2(size, size);
        }

        private void SpawnCards()
        {
            var totalCards = _rows * _columns;

            for (int i = 0; i < totalCards; i++)
            {
                var cardInstance = Instantiate(_cardPrefab, _boardArea);
                cardInstance.SetImmediateFaceUp(false);
                _cards.Add(cardInstance);
            }
        }

        #endregion
    }
}
