# DNStool 
.NET Framework tool to manage Active Directory Integrated DNS (ADIDNS)

## Examples

Add an A record
```shell
dnstool -action add -domain domain.corp -hostname corpserver01 -ip 10.0.0.10
[+] Add DNS record.
[+] Completed.
```

Modify an A record
```shell
dnstool -action modify -domain domain.corp -hostname corpserver01 -ip 10.0.0.11
[+] Modify DNS record.
[+] Completed.
```

View DNS record
```shell
dnstool -action view -domain domain.corp -hostname corpserver01
[+] View DNS record.
Record Type: 1, TTL: 30, Data: 10.0.0.11
```

Delete an A record
```shell
dnstool -action delete -domain domain.corp -hostname corpserver01
[+] Delete DNS record.
[+] Completed.
```

## Notes

* Only supports the creation/modification of A records