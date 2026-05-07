using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PnC.Runtime
{
    /// <summary>
    /// Room_Bedroom_B 전용: 버튼-팝업 매핑 및 열기/닫기.
    /// </summary>
    public sealed class BedroomBController : MonoBehaviour
    {
        private const string TargetSceneName = "Room_Bedroom_B";

        private const string PopupDrawing1Name = "Popup_Letter";
        private const string PopupCardName = "Popup_Card";
        private const string PopupPuzzleName = "Popup_Puzzle";

        private const string BtnTestPopupName = "Btn_TestPopup";
        private const string BtnPuzzleName = "Btn_Puzzle";
        private const string BtnCloseName = "Btn_Close";
        private const string DoorExitName = "Door_Exit";
        private const string VaultCanvasName = "VaultCanvas";
        private const string VaultConfirmName = "ConfirmButton";
        private const string NextSceneName = "Stage2Scene";

        private const string SolvedPuzzleVault = "puzzle_solved";

        private static readonly string[] CardOrder =
        {
            "쪽지 카드", "음료수 카드", "아이 카드", "건물카드"
        };

        private readonly HashSet<string> _solvedFlags = new HashSet<string>();
        private int _cardProgress;

        private GameObject _vaultCanvas;
        private GameObject _popupLetter;
        private GameObject _popupPuzzle;
        private GameObject activePopup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != TargetSceneName)
            {
                return;
            }

            var host = GameObject.Find("__BedroomBController") ?? new GameObject("__BedroomBController");
            var controller = host.GetComponent<BedroomBController>() ?? host.AddComponent<BedroomBController>();
            controller.BindScene();
        }

        public bool IsSolved(string id) => _solvedFlags.Contains(id);

        public void TryExit()
        {
            if (!IsSolved(SolvedPuzzleVault))
            {
                Debug.LogWarning("[BedroomB] Door locked until vault puzzle is solved.");
                return;
            }

            SceneManager.LoadScene(NextSceneName);
        }

        public void OnPuzzleSubmit(string input)
        {
            if (input != "0000")
            {
                return;
            }

            MarkSolved(SolvedPuzzleVault);
            OpenPopup(_popupPuzzle);
            _popupLetter?.SetActive(true);
        }

        public void OnCardClicked(PuzzleCardHandler card)
        {
            if (card == null || _cardProgress >= CardOrder.Length)
            {
                return;
            }

            if (card.gameObject.name == CardOrder[_cardProgress])
            {
                _cardProgress++;
                if (_cardProgress == CardOrder.Length)
                {
                    ClosePopup();
                }
            }
            else
            {
                _cardProgress = 0;
            }
        }

        public void OpenPopup(GameObject popup)
        {
            if (popup == null)
            {
                return;
            }

            if (activePopup != null && activePopup != popup)
            {
                activePopup.SetActive(false);
            }

            popup.SetActive(true);
            activePopup = popup;
        }

        public void ClosePopup()
        {
            if (activePopup == null)
            {
                return;
            }

            if (activePopup != _vaultCanvas)
            {
                activePopup.SetActive(false);
            }

            activePopup = null;
        }

        private void MarkSolved(string id) => _solvedFlags.Add(id);

        private void BindScene()
        {
            _vaultCanvas = GameObject.Find(VaultCanvasName);
            if (_vaultCanvas != null)
            {
                _vaultCanvas.SetActive(true);
            }

            var popupDrawing1 = GameObject.Find(PopupDrawing1Name);
            var popupCard = GameObject.Find(PopupCardName);
            var popupPuzzle = GameObject.Find(PopupPuzzleName);

            _popupLetter = popupDrawing1;
            _popupPuzzle = popupPuzzle;

            TryDeactivatePopup(popupDrawing1);
            TryDeactivatePopup(popupCard);
            TryDeactivatePopup(popupPuzzle);

            BindOpenButton(BtnTestPopupName, popupCard);
            BindOpenButton(BtnPuzzleName, popupDrawing1);
            BindCloseButton(BtnCloseName);
            BindExitDoor(DoorExitName);
            BindCardPuzzle(popupCard);
            BindVaultSubmit();
        }

        private static void TryDeactivatePopup(GameObject popup)
        {
            if (popup != null)
            {
                popup.SetActive(false);
            }
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                var deep = FindDeepChild(child, childName);
                if (deep != null)
                {
                    return deep;
                }
            }

            return null;
        }

        private void BindCardPuzzle(GameObject popupCard)
        {
            if (popupCard == null)
            {
                return;
            }

            var root = popupCard.transform;
            for (var i = 0; i < CardOrder.Length; i++)
            {
                var cardName = CardOrder[i];
                var tr = FindDeepChild(root, cardName);
                if (tr == null)
                {
                    Debug.LogWarning($"[BedroomB] Card '{cardName}' not found under '{PopupCardName}'.");
                    continue;
                }

                var oldHandler = tr.GetComponent<PopupClickHandler>();
                if (oldHandler != null)
                {
                    Destroy(oldHandler);
                }

                var handler = tr.GetComponent<PuzzleCardHandler>() ?? tr.gameObject.AddComponent<PuzzleCardHandler>();
                handler.Initialize(this, i);
            }
        }

        private void BindVaultSubmit()
        {
            if (_vaultCanvas == null)
            {
                return;
            }

            var field = _vaultCanvas.GetComponentInChildren<TMP_InputField>(true);
            if (field == null)
            {
                Debug.LogWarning($"[BedroomB] TMP_InputField (VaultInput) not found under '{VaultCanvasName}'.");
                return;
            }

            var confirmTr = FindDeepChild(_vaultCanvas.transform, VaultConfirmName);
            if (confirmTr == null)
            {
                Debug.LogWarning($"[BedroomB] '{VaultConfirmName}' not found under '{VaultCanvasName}'.");
                return;
            }

            var button = confirmTr.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[BedroomB] '{VaultConfirmName}' has no Button component.");
                return;
            }

            button.onClick.AddListener(() => OnPuzzleSubmit(field.text));
        }

        private void BindOpenButton(string buttonName, GameObject targetPopup)
        {
            var go = GameObject.Find(buttonName);
            if (go == null)
            {
                Debug.LogWarning($"[BedroomB] '{buttonName}' not found.");
                return;
            }

            if (targetPopup == null)
            {
                Debug.LogWarning($"[BedroomB] Popup for '{buttonName}' not found.");
                return;
            }

            var handler = go.GetComponent<PopupClickHandler>() ?? go.AddComponent<PopupClickHandler>();
            handler.Initialize(this, targetPopup, false);
        }

        private void BindCloseButton(string buttonName)
        {
            var go = GameObject.Find(buttonName);
            if (go == null)
            {
                Debug.LogWarning($"[BedroomB] '{buttonName}' not found.");
                return;
            }

            var handler = go.GetComponent<PopupClickHandler>() ?? go.AddComponent<PopupClickHandler>();
            handler.Initialize(this, null, false);
        }

        private void BindExitDoor(string buttonName)
        {
            var go = GameObject.Find(buttonName);
            if (go == null)
            {
                Debug.LogWarning($"[BedroomB] '{buttonName}' not found.");
                return;
            }

            var handler = go.GetComponent<PopupClickHandler>() ?? go.AddComponent<PopupClickHandler>();
            handler.Initialize(this, null, true);
        }
    }

    public sealed class PuzzleCardHandler : MonoBehaviour
    {
        private BedroomBController controller;
        private int expectedIndex;

        public void Initialize(BedroomBController owner, int index)
        {
            controller = owner;
            expectedIndex = index;
        }

        private void OnMouseDown()
        {
            if (controller != null)
            {
                controller.OnCardClicked(this);
            }
        }
    }

    /// <summary>
    /// OnMouseDown 시 팝업 열기, 닫기, 또는 출구 시도.
    /// </summary>
    public sealed class PopupClickHandler : MonoBehaviour
    {
        private BedroomBController controller;
        private GameObject targetPopup;
        private bool exitDoor;

        public void Initialize(BedroomBController owner, GameObject target, bool exitDoorMode = false)
        {
            controller = owner;
            targetPopup = target;
            exitDoor = exitDoorMode;
        }

        private void OnMouseDown()
        {
            if (controller == null)
            {
                return;
            }

            if (exitDoor)
            {
                controller.TryExit();
                return;
            }

            if (targetPopup != null)
            {
                controller.OpenPopup(targetPopup);
                return;
            }

            controller.ClosePopup();
        }
    }
}
