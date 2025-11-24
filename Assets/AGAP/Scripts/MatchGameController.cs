using UnityEngine;

namespace AGAP
{
    public class MatchGameController : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private BoardController _boardController;
        #endregion

        #region Unity Functions

        private void Start()
        {
            if (_boardController == null)
            {
                Debug.LogError("[MatchGameController] BoardController is not assigned.", this);
                return;
            }

            _boardController.BoardBuilt += OnBoardBuilt;

            if (_boardController.Cards.Count > 0)
                SubscribeToCards();
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
            SubscribeToCards();
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
            card.Flip(!card.IsFaceUp);
        }

        #endregion
    }
}