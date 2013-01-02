#
# ==============================================================
# @ID       $Id: FileSupport.psm1 1202 2012-03-27 02:40:19Z ww $
# @created  2011-08-25
# @project  http://cleancode.sourceforge.net/
# ==============================================================
#
# The official license for this file is shown next.
# Unofficially, consider this e-postcardware as well:
# if you find this module useful, let us know via e-mail, along with
# where in the world you are and (if applicable) your website address.
#
#
# ***** BEGIN LICENSE BLOCK *****
# Version: MPL 1.1
#
# The contents of this file are subject to the Mozilla Public License Version
# 1.1 (the "License"); you may not use this file except in compliance with
# the License. You may obtain a copy of the License at
# http://www.mozilla.org/MPL/
#
# Software distributed under the License is distributed on an "AS IS" basis,
# WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
# for the specific language governing rights and limitations under the
# License.
#
# The Original Code is part of the CleanCode toolbox.
#
# The Initial Developer of the Original Code is Michael Sorens.
# Portions created by the Initial Developer are Copyright (C) 2011
# the Initial Developer. All Rights Reserved.
#
# Contributor(s):
#
# ***** END LICENSE BLOCK *****
#

Set-StrictMode -Version Latest

Resolve-Path $PSScriptRoot\*.ps1 |
? { -not ($_.ProviderPath.Contains(".Tests.")) } |
% { . $_.ProviderPath }