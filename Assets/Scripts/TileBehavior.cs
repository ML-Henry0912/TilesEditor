using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilesEditor
{
    public class TileBehavior : MonoBehaviour
    {
        public Collider collider;


        public void Initialize()
        {
            collider.gameObject.tag = "Tile";
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
