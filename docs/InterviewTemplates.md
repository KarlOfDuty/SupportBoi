# Writing interview templates

The bot can automatically interview users when they open a ticket. These interviews are defined in interview templates. 
The templates each apply to a single ticket category so you can have different interviews depending on the type of ticket.

## Interview template commands

Whether you already have a template or not the first step is always to use the `/interviewtemplate get <category>` command on the 
ticket category you want to edit. If you haven't yet, use the `/addcategory` command on the category to register it with the bot.

The bot will reply with a JSON template file which you can edit and upload using the `/interviewtemplate set` command.

You can also delete an interview template using the `/interviewtemplate delete` command.

## Writing your first interview template

Use the get command to get a default template for the ticket category you want to edit:
```
/interviewtemplate get <category>
```

The bot will reply with a JSON template file for you to edit using your preferred text editor.

**Note:** It is highly recommended to use integrated template validation in your text editor, see below.

When you are done editing it you can upload it to the bot using the set command:
```
/interviewtemplate set <category>
```

The bot will check that your template is correctly formatted and provide feedback if it is not.

### Automatic validation and suggestions in your text editor

It is highly recommended to use the interview template JSON schema to get live validation of your template while you write it:

#### Guides for different editors:

<details>

<summary>VS Code</summary>

1. Go to `File->Preferences->Settings`.
2. Search for `json schema`.
3. Click `Edit in settings.json` on the schema setting.
4. Set the `json.schemas` property to the following to automatically validate template files:
```json
{
    "json.schemas":
    [
        {
            "fileMatch":
            [
                "interview-template*.json"
            ],
            "url": "https://raw.githubusercontent.com/KarlOfDuty/SupportBoi/refs/heads/main/Interviews/interview_template.schema.json"
        }
    ]
}
```
5. Open an interview template, you should now get suggestions for things like message types and color names, and error highlighting for any invalid sections.

</details>

<details>

<summary>Jetbrains Editors</summary>

1. Go to `File->Settings->Languages & Frameworks->Schemas->JSON Schema Mapping`.
2. Add a new schema with the following URL: `https://raw.githubusercontent.com/KarlOfDuty/SupportBoi/refs/heads/main/Interviews/interview_template.schema.json`.
   ![Schema settings](./img/riderJSONSchema.png)
3. Restart your editor and all interview templates should now automatically be set to the correct schema in the bottom right of the window.

</details>

![Auto Completion Example](./img/autoCompletionExample.png) ![Validation Example](./img/validationExample.png)

## Interview template format

This section lists all the properties that can be used in an interview template.
If you have set up your editor as suggested above it will handle a lot of this for you automatically.

All templates start with the following properties at the top level:

| Property&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Required | Description                                                                                                                          |
|----------------------------------------------------------------------------------------------------------|----------|--------------------------------------------------------------------------------------------------------------------------------------|
| `category-id`                                                                                            | Yes      | The id of the category this template applies to. You can change this and re-upload the template to apply it to a different category. |
| `interview`                                                                                              | Yes      | Contains the first step of the interview, see below.                                                                                 |

The rest of the template is a series of interview steps, with the following parameters:

| Property&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Required | Description                                                                                                                                                                                                                                                  |
|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `message`                                                                                                                                                                                                                                    | Yes      | The text in the embed message that will be sent to the user when they reach this step.                                                                                                                                                                       |
| `message-type`                                                                                                                                                                                                                               | Yes      | The type of message, decides what the bot will do when the user gets to this step.<br/>Must be one of: ERROR, END_WITH_SUMMARY, END_WITHOUT_SUMMARY, BUTTONS, TEXT_SELECTOR, USER_SELECTOR, ROLE_SELECTOR,MENTIONABLE_SELECTOR, CHANNEL_SELECTOR, TEXT_INPUT |
| `color`                                                                                                                                                                                                                                      | Yes      | Colour of the message embed.                                                                                                                                                                                                                                 |
| `paths`                                                                                                                                                                                                                                      | Yes      | One or more interview steps. The name of the step is used as a regex match against the user's answer, except for selection boxes and buttons where each step becomes a button or selection option.                                                           |
| `heading`                                                                                                                                                                                                                                    | No       | The title of the embed message.                                                                                                                                                                                                                              |
| `summary-field`                                                                                                                                                                                                                              | No       |                                                                                                                                                                                                                                                              |
| `button-style`                                                                                                                                                                                                                               | No       |                                                                                                                                                                                                                                                              |
| `selector-description`                                                                                                                                                                                                                       | No       |                                                                                                                                                                                                                                                              |
| `selector-placeholder`                                                                                                                                                                                                                       | No       |                                                                                                                                                                                                                                                              |
| `max-length`                                                                                                                                                                                                                                 | No       |                                                                                                                                                                                                                                                              |
| `min-length`                                                                                                                                                                                                                                 | No       |                                                                                                                                                                                                                                                              |


## Example template

```json
{
  "category-id": "1005612326340272169",
  "interview":
  {
    "message": "Are you appealing your own ban or on behalf of another user?",
    "message-type": "BUTTONS",
    "color": "AQUAMARINE",
    "paths":
    {
      "My own ban":
      {
        "message": "Please write your appeal below, motivate why you think you should be unbanned.",
        "message-type": "TEXT_INPUT",
        "color": "CYAN",
        "summary-field": "Ban appeal",
        "paths":
        {
          ".*":
          {
            "heading": "Appeal Summary",
            "message": "Thank you, a staff member will review your appeal.",
            "message-type": "END_WITH_SUMMARY",
            "color": "GREEN",
            "paths": {}
          }
        }
      },
      "Another user's ban":
      {
        "message": "Whose ban are you appealing?",
        "message-type": "USER_SELECTOR",
        "color": "CYAN",
        "summary-field": "User",
        "paths":
        {
          "<@170904988724232192>":
          {
            "message": "Not allowed",
            "message-type": "ERROR",
            "color": "RED",
            "paths": {}
          },
          ".*":
          {
            "message": "What is their role?",
            "message-type": "ROLE_SELECTOR",
            "color": "CYAN",
            "summary-field": "Their Role",
            "paths":
            {
              ".*":
              {
                "message": "Please write the appeal below.",
                "message-type": "TEXT_INPUT",
                "color": "CYAN",
                "summary-field": "Ban appeal",
                "paths":
                {
                  ".*":
                  {
                    "heading": "Appeal Summary",
                    "message": "Thank you, a staff member will review the appeal.",
                    "message-type": "END_WITH_SUMMARY",
                    "color": "GREEN",
                    "paths": {}
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```