// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("Ujo9iqGcLrivpUSPfiuYBYWpyt2nFZa1p5qRnr0R3xFgmpaWlpKXlD2AWa3YwcVnOl/MuB8ZwyeFLsSGpMlEUik0oU0VoxCqerA0L9oGnowTbIE7o9Zp+yg5zgzRkwh4LBI0Zr59zfzBy20PTyqMQypYv7MbtZ5OFZaYl6cVlp2VFZaWlyWS7uRTE3AFj4hOb0W1xrCVrM2PhC52LElIVP00GzOjMF5RyOKnlaVG2VTV7lMBZnJoBSdKsR45JGnHelD0ddOUnwU4lreGPFLqmeVqHbfN2CmNN1n0yrECCVI674QUJhne+J+g6eeUxwYB2DRo7158nmhjkPcy7wbA8VALtRYC0gLZvMTNWzJMoYtg07Qx63OUge4rI8UWjrkLUpWUlpeW");
        private static int[] order = new int[] { 5,5,2,8,12,8,8,8,13,12,11,13,13,13,14 };
        private static int key = 151;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
