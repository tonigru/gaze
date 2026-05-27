using GazeCraft;
using UnityEngine;

public sealed class KeywordScript : MonoBehaviour
{
    private GazeCraftGameManager manager;

    private void Awake()
    {
        manager = FindAnyObjectByType<GazeCraftGameManager>();
    }

    public void TakeThat()
    {
        manager ??= FindAnyObjectByType<GazeCraftGameManager>();
        manager?.TakeThat();
    }

    public void PutThat()
    {
        manager ??= FindAnyObjectByType<GazeCraftGameManager>();
        manager?.PutThat();
    }
}
