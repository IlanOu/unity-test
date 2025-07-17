using UnityEngine;

public interface ILoadingBar
{
    void Show();
    void Hide();
    void UpdateProgress(float progress);
    void SetPosition(Vector3 position);
}