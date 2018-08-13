@echo off
pushd "%~dp0"

@REM Build POT file
if exist Nursery.edit.pot (
  msgcat --no-location --output Nursery.pot Nursery.edit.pot
)
if exist BasicPlugins.edit.pot (
  msgcat --no-location --output BasicPlugins.pot BasicPlugins.edit.pot
)
if exist SoundEffectPlugin.edit.pot (
  msgcat --no-location --output SoundEffectPlugin.pot SoundEffectPlugin.edit.pot
)
if exist UserDefinedFilterPlugin.edit.pot (
  msgcat --no-location --output UserDefinedFilterPlugin.pot UserDefinedFilterPlugin.edit.pot
)
if exist UserDefinedSchedulerPlugin.edit.pot (
  msgcat --no-location --output UserDefinedSchedulerPlugin.pot UserDefinedSchedulerPlugin.edit.pot
)

@REM Build minimized PO file
if exist Nursery.ja_JP.edit.po (
  msgcat --no-location --output Nursery.ja_JP.po Nursery.ja_JP.edit.po
)
if exist BasicPlugins.ja_JP.edit.po (
  msgcat --no-location --output BasicPlugins.ja_JP.po BasicPlugins.ja_JP.edit.po
)
if exist SoundEffectPlugin.ja_JP.edit.po (
  msgcat --no-location --output SoundEffectPlugin.ja_JP.po SoundEffectPlugin.ja_JP.edit.po
)
if exist UserDefinedFilterPlugin.ja_JP.edit.po (
  msgcat --no-location --output UserDefinedFilterPlugin.ja_JP.po UserDefinedFilterPlugin.ja_JP.edit.po
)
if exist UserDefinedSchedulerPlugin.ja_JP.edit.po (
  msgcat --no-location --output UserDefinedSchedulerPlugin.ja_JP.po UserDefinedSchedulerPlugin.ja_JP.edit.po
)

@REM Build MO file
mkdir locale\ja_JP\LC_MESSAGES > NUL 2>&1
msgfmt --output-file locale\ja_JP\LC_MESSAGES\Nursery.mo Nursery.ja_JP.po
msgfmt --output-file locale\ja_JP\LC_MESSAGES\BasicPlugins.mo BasicPlugins.ja_JP.po
msgfmt --output-file locale\ja_JP\LC_MESSAGES\SoundEffectPlugin.mo SoundEffectPlugin.ja_JP.po
msgfmt --output-file locale\ja_JP\LC_MESSAGES\UserDefinedFilterPlugin.mo UserDefinedFilterPlugin.ja_JP.po
msgfmt --output-file locale\ja_JP\LC_MESSAGES\UserDefinedSchedulerPlugin.mo UserDefinedSchedulerPlugin.ja_JP.po

@REM Remove files
del *.mo 2>NUL

popd
