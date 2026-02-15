# Azure Durable Function Sandbox Project

- Getting the Funciton Key

```bash
az functionapp function keys list --name func-employee-durable-demo --resource-group Azure_Durable_Function_Demo --function-name EmployeeHttpTrigger --query default -o tsv
```

- Creating and employee

```bash     
  curl -X POST "https://func-employee-durable-demo-fchzbygjbje5bkd8.canadacentral-01.azurewebsites.net/api/employee/process?code=<FUNC_KEY>" \
    -H "Content-Type: application/json" \
    -d '{
      "clientId": "ACME-001",
      "employeeId": "EMP-DEMO-001",
      "employeeName": "John Anderson",
      "employeeAge": 35,
      "email": "sumalsurendra1999@gmail.com",
      "employeeCertificates": [
        {"certificateId": "AZ-900", "certificateName": "Azure Fundamentals"},
        {"certificateId": "AZ-204", "certificateName": "Azure Developer Associate"}
      ]
    }'
```


## Important

- SAS URL - short lived URLs for sharing the content.
- Used sqlcmd as the CLI tool for SQL stuff.

