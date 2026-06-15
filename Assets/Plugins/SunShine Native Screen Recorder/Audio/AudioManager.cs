using UnityEngine;

public class AudioManager : MonoBehaviour
{
    
    public AudioSource audioSource;


    public void PlaySound()
    {
        audioSource.Play();
    }

    public void stopSound()
    {
        audioSource.Stop();
    }
}
