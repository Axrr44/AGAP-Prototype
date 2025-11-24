using System;
using System.Collections.Generic;
using DG.Tweening;
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

        [Header("Animation")]
        [SerializeField] private float _spawnDuration = 0.25f;
        [SerializeField] private float _spawnStagger = 0.03f;

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
        private readonly List<int> _cardIds = new List<int>();
        private bool _useSavedLayout;
        private readonly List<int> _savedCardIds = new List<int>();
        private readonly List<bool> _savedMatchedFlags = new List<bool>();
        private readonly Dictionary<int, Color> _colorById = new Dictionary<int, Color>();

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

        #endregion

        #region Public Functions

        public void SetLayout(int rows, int columns)
        {
            _rows = Mathf.Max(1, rows);
            _columns = Mathf.Max(1, columns);
        }

        public void BuildBoard()
        {
            if (_boardArea == null || _gridLayout == null || _cardPrefab == null)
            {
                Debug.LogError("[BoardController] Cannot build board – missing references.", this);
                return;
            }

            ClearExistingCards();
            ConfigureLayout();

            if (_useSavedLayout)
            {
                SpawnCardsFromSaved();
                _useSavedLayout = false;
            }
            else
            {
                PrepareCardIds();
                SpawnCards();
            }

            BoardBuilt?.Invoke();
        }

        public void BuildBoardFromSave(int rows, int columns, IList<int> cardIds, IList<bool> matchedFlags)
        {
            _rows = rows;
            _columns = columns;

            _savedCardIds.Clear();
            _savedMatchedFlags.Clear();

            if (cardIds != null)
                _savedCardIds.AddRange(cardIds);

            if (matchedFlags != null)
                _savedMatchedFlags.AddRange(matchedFlags);

            _useSavedLayout = true;
            BuildBoard();
        }

        #endregion

        #region Private Functions

        private void ClearExistingCards()
        {
            _cards.Clear();
            _cardIds.Clear();
            _savedCardIds.Clear();
            _savedMatchedFlags.Clear();
            _colorById.Clear();

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

        private void PrepareCardIds()
        {
            var totalCards = _rows * _columns;

            if (totalCards < 2)
                return;

            var pairCount = totalCards / 2;

            for (int i = 0; i < pairCount; i++)
            {
                _cardIds.Add(i);
                _cardIds.Add(i);
            }

            if (_cardIds.Count < totalCards)
                _cardIds.Add(pairCount);

            for (int i = 0; i < _cardIds.Count; i++)
            {
                var j = UnityEngine.Random.Range(i, _cardIds.Count);
                (_cardIds[i], _cardIds[j]) = (_cardIds[j], _cardIds[i]);
            }
        }

        private void SpawnCards()
        {
            var totalCards = _rows * _columns;

            for (int i = 0; i < totalCards; i++)
            {
                var cardInstance = Instantiate(_cardPrefab, _boardArea);
                cardInstance.SetImmediateFaceUp(false);

                if (i < _cardIds.Count)
                {
                    var id = _cardIds[i];
                    cardInstance.SetCardId(id);
                    cardInstance.SetFrontColor(GetColorForId(id));
                }

                _cards.Add(cardInstance);

                var rect = (RectTransform)cardInstance.transform;
                rect.localScale = Vector3.zero;
                rect.DOScale(Vector3.one, _spawnDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(_spawnStagger * i);
            }
        }

        private void SpawnCardsFromSaved()
        {
            var totalCards = _rows * _columns;

            for (int i = 0; i < totalCards; i++)
            {
                var cardInstance = Instantiate(_cardPrefab, _boardArea);
                cardInstance.SetImmediateFaceUp(false);

                if (i < _savedCardIds.Count)
                {
                    var id = _savedCardIds[i];
                    cardInstance.SetCardId(id);
                    cardInstance.SetFrontColor(GetColorForId(id));
                }

                if (i < _savedMatchedFlags.Count && _savedMatchedFlags[i])
                {
                    cardInstance.SetMatched(true);
                    cardInstance.SetImmediateFaceUp(true);
                }

                _cards.Add(cardInstance);

                var rect = (RectTransform)cardInstance.transform;
                rect.localScale = Vector3.zero;
                rect.DOScale(Vector3.one, _spawnDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(_spawnStagger * i);
            }
        }

        private Color GetColorForId(int id)
        {
            if (_colorById.TryGetValue(id, out var color))
                return color;

            float h = (id * 0.161f) % 1f;
            color = Color.HSVToRGB(h, 0.6f, 0.9f);
            _colorById[id] = color;
            return color;
        }

        #endregion
    }
}
