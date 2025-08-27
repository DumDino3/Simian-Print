
using UnityEngine;
using UnityEngine.SceneManagement;

public class Script : MonoBehaviour
{
    public Transform player;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) )
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        this.transform.position = new Vector3(player.position.x, player.position.y + 0.5f, this.transform.position.z);
    }
}
