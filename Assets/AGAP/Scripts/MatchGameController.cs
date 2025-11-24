using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AGAP
{
    public class MatchGameController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private BoardController _boardController;
        [SerializeField] private float _mismatchFlipBackDelay = 0.6f;
        [SerializeField] private Text _scoreText;
        [SerializeField] private int _matchScoreValue = 10;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _flipClip;
        [SerializeField] private AudioClip _matchClip;
        [SerializeField] private AudioClip _mismatchClip;
        [SerializeField] private AudioClip _gameOverClip;

        #endregion

        #region Private Fields

        private readonly List<CardController> _currentPair = new List<CardController>(2);
        private readonly HashSet<CardController> _lockedCards = new HashSet<CardController>();
        private int _score;
        private bool _isGameOver;

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

            if (_boardController.Cards.Count > 0)
                SubscribeToCards();

            ResetState();
        }

        private void OnDestroy()
        {
            if (_boardController != null)
                _boardController.BoardBuilt -= OnBoardBuilt;

            UnsubscribeFromCards();
        }

        #endregion

        #region Private Functions

        private void OnBoardBuilt()
        {
            UnsubscribeFromCards();
            _currentPair.Clear();
            _lockedCards.Clear();
            SubscribeToCards();
            ResetState();
        }

        private void ResetState()
        {
            _isGameOver = false;
            ResetScore();
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
                return;

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
                return;
            }

            PlayClip(_mismatchClip);
            StartCoroutine(FlipBackPair(first, second));
            _currentPair.Clear();
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
        }

        private void ResetScore()
        {
            _score = 0;
            UpdateScoreText();
        }

        private void AddScore(int value)
        {
            _score += value;
            UpdateScoreText();
        }

        private void UpdateScoreText()
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {_score}";
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
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource == null)
                return;

            if (clip == null)
                return;

            _audioSource.PlayOneShot(clip);
        }

        #endregion
    }
}
