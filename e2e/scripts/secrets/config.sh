#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.

DD_VAULT_ADDR=https://vault.us1.ddbuild.io
DD_SDK_MAUI_SECRETS_PATH_PREFIX='kv/aws/arn:aws:iam::486234852809:role/ci-dd-sdk-maui'

DD_MAUI_E2E_CLIENT_TOKEN="e2e.client.token"
DD_MAUI_E2E_API_KEY="e2e.api.key"
DD_MAUI_E2E_APP_KEY="e2e.app.key"
DD_MAUI_E2E_SYNTHETICS_ANDROID_APP_ID="e2e.synthetics.android.app.id"
DD_MAUI_E2E_SSH_KEY="ssh.key"
