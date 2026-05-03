import fs from "fs";
import OpenAI from "openai";

const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

async function fixCode(file) {
  const code = fs.readFileSync(file, "utf-8");

  const res = await client.chat.completions.create({
    model: "gpt-4.1",
    messages: [
      {
        role: "system",
        content: "Fix code errors and return clean working code only"
      },
      {
        role: "user",
        content: code
      }
    ]
  });

  fs.writeFileSync(file, res.choices[0].message.content);
  console.log("✔ Fixed:", file);
}

import fs from "fs";
import OpenAI from "openai";

const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

async function fixCode(file) {
  const code = fs.readFileSync(file, "utf-8");

  const res = await client.chat.completions.create({
    model: "gpt-4.1",
    messages: [
      {
        role: "system",
        content: "Fix code errors and return clean working code only"
      },
      {
        role: "user",
        content: code
      }
    ]
  });

  fs.writeFileSync(file, res.choices[0].message.content);
  console.log("✔ Fixed:", file);
}

fixCode("./app.js"); 
