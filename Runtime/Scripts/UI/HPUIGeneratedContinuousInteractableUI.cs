using ubco.ovilab.HPUI.Core.Tracking;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;
using TMPro;

namespace ubco.ovilab.HPUI.Core.UI
{
    public class HPUIGeneratedContinuousInteractableUI : MonoBehaviour
    {
        [SerializeField] private JointFollower jointFollower;
        [SerializeField] private Transform UIRoot;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Transform inProgressObj;
        [SerializeField] private TMP_Text textMessage;

        private bool usingInProgress = false;

        /// <summary>
        /// Above which hand is the UI expected to show?
        /// </summary>
        public Handedness Handedness
        {
            get => jointFollower?.JointFollowerDatumProperty.Value.handedness ?? Handedness.Invalid;
            set {
                if (jointFollower != null)
                {
                    jointFollower.JointFollowerDatumProperty.Value.handedness = value;
                }
            }
        }

        /// <summary>
        /// The text message to display.
        /// </summary>
        public string TextMessage
        {
            get => textMessage.text;
            set => textMessage.text = value;
        }

        /// <summary>
        /// Set the progress bar ratio. Expecting to be a value between 0 and 1.
        /// This wil also disable the in progress visual.
        /// </summary>
        public void SetProgress(float progress)
        {
            usingInProgress = false;
            progressBarImage.transform.parent.gameObject.SetActive(true);
            inProgressObj.gameObject.SetActive(false);
            progressBarImage.fillAmount = progress;
        }

        /// <summary>
        /// Show the in progress visual.
        /// This wil also disable the progress bar visual.
        /// </summary>
        public void InProgress()
        {
            usingInProgress = true;
            progressBarImage.transform.parent.gameObject.SetActive(false);
            inProgressObj.gameObject.SetActive(true);
        }

        /// <inheritdoc />
        private void Update()
        {
            if (usingInProgress)
            {
                // Full rotation every 3 seconds.
                inProgressObj.Rotate(0, (Time.time % 3) / 3 * 360, 0);
            }

            UIRoot.localPosition = transform.position + Vector3.up * 0.1f;
            UIRoot.LookAt(Camera.main.transform);
        }

        /// <summary>
        /// Show the UI
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the UI
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
