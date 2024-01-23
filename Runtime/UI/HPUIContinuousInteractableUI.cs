using ubco.ovilab.HPUI.Tracking;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.UI
{
    public class HPUIContinuousInteractableUI : MonoBehaviour
    {
        private const string UIPrefab = "Packages/ubc.ok.ovilab.hpui-core/Runtime/Prefabs/HPUIContinousUI.prefab";
        [SerializeField] private JointFollower jointFollower;
        [SerializeField] private Transform UIRoot;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Image inProgressImage;
        [SerializeField] private Text textMessage;

        private bool usingInProgress = false;

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

        public string TextMessage {
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
            inProgressImage.transform.parent.gameObject.SetActive(false);
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
            inProgressImage.transform.parent.gameObject.SetActive(true);
        }

        /// <inheritdoc />
        private void Update()
        {
            if (usingInProgress)
            {
                // Full rotation every 3 seconds.
                inProgressImage.fillAmount = Time.time / 3;
            }

            UIRoot.localPosition = transform.position + Vector3.up * 0.1f;
            UIRoot.LookAt(Camera.main.transform);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
