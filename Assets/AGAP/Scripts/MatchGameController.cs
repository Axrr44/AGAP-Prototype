using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AGAP
{
    public class MatchGameController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private BoardController _boardController;
        [SerializeField] private float _mismatchFlipBackDelay = 0.6f;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private int _matchScoreValue = 10;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _flipClip;
        [SerializeField] private AudioClip _matchClip;
        [SerializeField] private AudioClip _mismatchClip;
        [SerializeField] private AudioClip _gameOverClip;

        [Header("UI")]
        [SerializeField] private GameObject _startPanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private Slider _layoutSlider;
        [SerializeField] private TextMeshProUGUI _layoutLabel;

        [Header("Layouts")]
        [SerializeField] private Vector2Int[] _layoutOptions =
        {
            new Vector2Int(2, 2),
            new Vector2Int(2, 3),
            new Vector2Int(3, 4),
            new Vector2Int(4, 4),
            new Vector2Int(5, 6)
        };

        #endregion

        #region Private Fields

        private readonly List<CardController> _currentPair = new List<CardController>(2);
        private readonly HashSet<CardController> _lockedCards = new HashSet<CardController>();
        private int _score;
        private bool _isGameOver;
        private Tween _scoreTween;

        private const string SaveKey = "AGAP_MATCH_SAVE";

        #endregion

        #region Unity Functions

        private void Start()
        {
            if (_boardController == null)
            {
                Debug.LogError("[MatchGameController] BoardController is not assigned.", this);
                return;
            }

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            _boardController.BoardBuilt += OnBoardBuilt;

            if (_startPanel != null && _startButton != null)
            {
                _startPanel.SetActive(true);

                if (_gameOverPanel != null)
                    _gameOverPanel.SetActive(false);

                _startButton.onClick.AddListener(OnStartClicked);

                if (_retryButton != null)
                    _retryButton.onClick.AddListener(OnRetryClicked);

                if (_layoutSlider != null && _layoutOptions != null && _layoutOptions.Length > 0)
                {
                    _layoutSlider.wholeNumbers = true;
                    _layoutSlider.minValue = 0;
                    _layoutSlider.maxValue = _layoutOptions.Length - 1;
                    _layoutSlider.value = 0;
                    UpdateLayoutLabel(0);
                    _layoutSlider.onValueChanged.AddListener(OnLayoutSliderChanged);
                }

                UpdateScoreText();
                return;
            }

            StartNewOrLoad();
        }

        private void OnDestroy()
        {
            if (_boardController != null)
                _boardController.BoardBuilt -= OnBoardBuilt;

            UnsubscribeFromCards();

            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStartClicked);

            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(OnRetryClicked);

            if (_layoutSlider != null)
                _layoutSlider.onValueChanged.RemoveListener(OnLayoutSliderChanged);
        }

        #endregion

        #region Private Functions

        private void OnStartClicked()
        {
            if (_startPanel != null)
                _startPanel.SetActive(false);

            ClearSave();

            var layoutIndex = 0;

            if (_layoutSlider != null && _layoutOptions != null && _layoutOptions.Length > 0)
                layoutIndex = Mathf.Clamp(Mathf.RoundToInt(_layoutSlider.value), 0, _layoutOptions.Length - 1);

            var layout = _layoutOptions != null && _layoutOptions.Length > 0
                ? _layoutOptions[layoutIndex]
                : new Vector2Int(2, 2);

            _boardController.SetLayout(layout.x, layout.y);
            _boardController.BuildBoard();
            ResetState();
        }

        private void OnRetryClicked()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            ClearSave();
            _boardController.BuildBoard();
            ResetState();
        }

        private void OnLayoutSliderChanged(float value)
        {
            var index = Mathf.Clamp(Mathf.RoundToInt(value), 0, _layoutOptions.Length - 1);
            UpdateLayoutLabel(index);
        }

        private void UpdateLayoutLabel(int index)
        {
            if (_layoutLabel == null)
                return;

            if (_layoutOptions == null || _layoutOptions.Length == 0)
            {
                _layoutLabel.text = string.Empty;
                return;
            }

            index = Mathf.Clamp(index, 0, _layoutOptions.Length - 1);
            var layout = _layoutOptions[index];
            _layoutLabel.text = $"Layout: {layout.x} x {layout.y}";
        }

        private void StartNewOrLoad()
        {
            if (!TryLoadGame())
            {
                _boardController.BuildBoard();
                ResetState();
            }
        }

        private void OnBoardBuilt()
        {
            UnsubscribeFromCards();
            _currentPair.Clear();
            _lockedCards.Clear();
            SubscribeToCards();
            UpdateScoreText();
        }

        private void ResetState()
        {
            _isGameOver = false;
            _score = 0;
            UpdateScoreText();
            SaveGame();
        }

        private void SubscribeToCards()
        {
            foreach (var card in _boardController.Cards)
                card.Clicked += OnCardClicked;
        }

        private void UnsubscribeFromCards()
        {
            if (_boardController == null || _boardController.Cards == null)
                return;

            foreach (var card in _boardController.Cards)
                card.Clicked -= OnCardClicked;
        }

        private void OnCardClicked(CardController card)
        {
            if (card == null)
                return;

            if (_isGameOver)
                return;

            if (card.IsMatched)
                return;

            if (card.IsFaceUp)
                return;

            if (_lockedCards.Contains(card))
                return;

            PlayClip(_flipClip);
            card.Flip(true);
            _currentPair.Add(card);

            if (_currentPair.Count < 2)
            {
                SaveGame();
                return;
            }

            var first = _currentPair[0];
            var second = _currentPair[1];

            if (first.CardId == second.CardId)
            {
                first.SetMatched(true);
                second.SetMatched(true);
                AddScore(_matchScoreValue);
                PlayClip(_matchClip);
                _currentPair.Clear();
                CheckGameOver();
                SaveGame();
                return;
            }

            PlayClip(_mismatchClip);
            StartCoroutine(FlipBackPair(first, second));
            _currentPair.Clear();
            SaveGame();
        }

        private IEnumerator FlipBackPair(CardController first, CardController second)
        {
            _lockedCards.Add(first);
            _lockedCards.Add(second);

            yield return new WaitForSeconds(_mismatchFlipBackDelay);

            if (!first.IsMatched)
                first.Flip(false);

            if (!second.IsMatched)
                second.Flip(false);

            _lockedCards.Remove(first);
            _lockedCards.Remove(second);

            SaveGame();
        }

        private void AddScore(int value)
        {
            _score += value;
            UpdateScoreText();
            PulseScore();
        }

        private void UpdateScoreText()
        {
            if (_scoreText != null)
                _scoreText.text = _score.ToString();
        }

        private void PulseScore()
        {
            if (_scoreText == null)
                return;

            _scoreTween?.Kill();

            var rect = _scoreText.rectTransform;
            rect.localScale = Vector3.one;

            _scoreTween = rect.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.2f, 1);
        }

        private void CheckGameOver()
        {
            foreach (var card in _boardController.Cards)
            {
                if (!card.IsMatched)
                    return;
            }

            _isGameOver = true;
            PlayClip(_gameOverClip);
            ClearSave();

            if (_finalScoreText != null)
                _finalScoreText.text = $"Score: {_score}";

            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(true);
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource == null)
                return;

            if (clip == null)
                return;

            _audioSource.PlayOneShot(clip);
        }

        private void SaveGame()
        {
            if (_boardController == null || _boardController.Cards == null || _boardController.Cards.Count == 0)
                return;

            var data = new MatchGameSaveData
            {
                rows = _boardController.Rows,
                columns = _boardController.Columns,
                score = _score
            };

            int count = _boardController.Cards.Count;
            data.cardIds = new int[count];
            data.matchedFlags = new bool[count];

            for (int i = 0; i < count; i++)
            {
                var card = _boardController.Cards[i];
                data.cardIds[i] = card.CardId;
                data.matchedFlags[i] = card.IsMatched;
            }

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private bool TryLoadGame()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return false;

            var json = PlayerPrefs.GetString(SaveKey);
            if (string.IsNullOrEmpty(json))
                return false;

            MatchGameSaveData data;

            try
            {
                data = JsonUtility.FromJson<MatchGameSaveData>(json);
            }
            catch
            {
                return false;
            }

            if (data == null)
                return false;

            if (data.rows <= 0 || data.columns <= 0)
                return false;

            if (data.cardIds == null || data.matchedFlags == null)
                return false;

            if (data.cardIds.Length != data.matchedFlags.Length)
                return false;

            _score = data.score;
            _isGameOver = false;

            _boardController.BuildBoardFromSave(
                data.rows,
                data.columns,
                data.cardIds,
                data.matchedFlags
            );

            UpdateScoreText();
            return true;
        }

        private void ClearSave()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return;

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
