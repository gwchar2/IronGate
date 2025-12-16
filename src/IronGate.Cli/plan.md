# PLAN



#TODO
- [ ] Research what are the prerequisites for executing brute force attacks against APIs. (Do I need a file of passwords? Do I need a list of usernames?)
- [ ] Research how to execute brute force attacks against APIs.
- [ ] What arguments do we receive from the CLI for brute forcing APIs? just `attack brute <username>` ?
- [ ] Implement brute forcing APIs.
- [ ] Research what are the prerequisites for executing password spraying attacks against APIs. (Do I need a file of passwords? Do I need a list of usernames?)
- [ ] Research how to execute password spraying attacks against APIs.
- [ ] What arguments do we receive from the CLI for password spraying APIs? just `attack spray <usernames_file>` ?
- [ ] Implement password spraying APIs.

- [ ] After implementing brute force and password spraying, create a test combination set of defences and attacks.
		(Combinations such as: SHA256+salt + totp + captcha, BCRYPT + Pepper + lockout, etc')

		
# What I am thinking so far:

- Brute forcing APIs would require a username and a password list. We can use rockyou.txt for that.
  We can do multiple attempts with different password strengths (easy medium hard), and compare results.
- Password spraying APIs would require a usernames file and a password. We can use rockyou.txt for that. 
  We can try a different set of accounts each time. Each set of users will have the same password strength (easy medium hard), and compare results.