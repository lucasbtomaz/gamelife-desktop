import { useState } from 'react';
import {
  SERVICOS_ASSINATURA,
  SITUACOES_ASSINATURA,
  excluirAssinatura,
  formatarBadgeAssinatura,
  formatarDataAssinatura,
  obterAssinaturas
} from '../api/assinaturas.js';

const estadoPlataforma = () => ({
  compraDigital: true,
  midiaFisica: false,
  porAssinatura: false,
  acompanhar: false,
  status: '',
  dataStatus: '',
  tempoEmHoras: '',
  conquistasPossiveis: '',
  conquistasObtidas: ''
});

const numeroOpcional = (valor) => valor === '' ? undefined : Number(valor);
const valorCampo = (valor) => valor == null ? '' : String(valor);
const paraInputDate = (valor) => (valor ? String(valor).slice(0, 10) : '');
const chavePlataforma = (valor) => valor.trim().normalize('NFKC').toLocaleUpperCase('pt-BR');

const criarOpcoesPlataforma = (platforms, jogo) => {
  const opcoes = [...platforms];
  for (const plataforma of jogo?.plataformas ?? []) {
    if (!opcoes.some((opcao) => chavePlataforma(opcao) === chavePlataforma(plataforma.nome))) {
      opcoes.push(plataforma.nome);
    }
  }
  return opcoes;
};

const criarEstadoInicial = (jogo, opcoes) => Object.fromEntries((jogo?.plataformas ?? []).map((plataforma) => {
  const acompanhamento = plataforma.acompanhamento;
  const nomeOpcao = opcoes.find((opcao) => chavePlataforma(opcao) === chavePlataforma(plataforma.nome)) ?? plataforma.nome;
  const midiaFisica = Boolean(plataforma.midiaFisica);
  const porAssinatura = Boolean(plataforma.porAssinatura);
  const compraDigital = plataforma.compraDigital ?? (!midiaFisica && !porAssinatura);
  return [nomeOpcao, {
    compraDigital,
    midiaFisica,
    porAssinatura,
    acompanhar: Boolean(acompanhamento),
    status: acompanhamento?.status ?? '',
    dataStatus: acompanhamento?.dataStatus?.slice(0, 10) ?? '',
    tempoEmHoras: valorCampo(acompanhamento?.tempoEmHoras),
    conquistasPossiveis: valorCampo(acompanhamento?.conquistasPossiveis),
    conquistasObtidas: valorCampo(acompanhamento?.conquistasObtidas)
  }];
}));

const criarRascunhoAssinatura = (assinatura = null) => ({
  id: assinatura?.id ?? null,
  servico: assinatura?.servico ?? SERVICOS_ASSINATURA[0],
  situacao: assinatura?.situacao ?? SITUACOES_ASSINATURA[1],
  dataDisponibilidade: paraInputDate(assinatura?.dataDisponibilidade),
  dataEncerramento: paraInputDate(assinatura?.dataEncerramento)
});

function LibraryGameForm({ platforms, saving, onSubmit, initialGame = null, onCancel }) {
  const opcoesPlataforma = criarOpcoesPlataforma(platforms, initialGame);
  const [titulo, setTitulo] = useState(() => initialGame?.titulo ?? '');
  const [anoLancamento, setAnoLancamento] = useState(() => valorCampo(initialGame?.anoLancamento));
  const [selecionadas, setSelecionadas] = useState(() => criarEstadoInicial(initialGame, opcoesPlataforma));
  const [assinaturas, setAssinaturas] = useState(() => (initialGame?.assinaturas ?? []).map((item) => ({ ...item })));
  const [assinaturasRemovidas, setAssinaturasRemovidas] = useState([]);
  const [editorAssinatura, setEditorAssinatura] = useState(null);
  const [assinaturasEditadasPeloUsuario, setAssinaturasEditadasPeloUsuario] = useState(false);
  const [erro, setErro] = useState('');

  const editando = Boolean(initialGame);
  const plataformasOriginais = new Set((initialGame?.plataformas ?? []).map((plataforma) => chavePlataforma(plataforma.nome)));
  const temAlgumaAssinaturaMarcada = Object.values(selecionadas).some((dados) => dados.porAssinatura);
  const exibirSecaoAssinaturas = temAlgumaAssinaturaMarcada || assinaturas.length > 0 || editorAssinatura !== null;

  const carregarAssinaturasCompartilhadas = async () => {
    if (editando || assinaturasEditadasPeloUsuario) return;
    const tituloLimpo = titulo.trim();
    if (!tituloLimpo) return;
    try {
      const lista = await obterAssinaturas({ titulo: tituloLimpo });
      if (Array.isArray(lista) && lista.length > 0) {
        setAssinaturas(lista);
      }
    } catch {
      // Consulta opcional de compartilhamento; falha silenciosa não bloqueia cadastro.
    }
  };

  const alternarPlataforma = (plataforma, marcada) => {
    setSelecionadas((atuais) => {
      if (marcada) return { ...atuais, [plataforma]: estadoPlataforma() };
      const proximas = { ...atuais };
      delete proximas[plataforma];
      return proximas;
    });
    setErro('');
  };

  const atualizarPlataforma = (plataforma, campo, valor) => {
    setSelecionadas((atuais) => ({
      ...atuais,
      [plataforma]: { ...atuais[plataforma], [campo]: valor }
    }));
    if (campo === 'porAssinatura' && valor && assinaturas.length === 0 && !editorAssinatura) {
      setEditorAssinatura(criarRascunhoAssinatura());
    }
    setErro('');
  };

  const confirmarRascunhoAssinatura = () => {
    if (!editorAssinatura) return null;
    if (
      editorAssinatura.dataDisponibilidade &&
      editorAssinatura.dataEncerramento &&
      editorAssinatura.dataEncerramento < editorAssinatura.dataDisponibilidade
    ) {
      setErro('A data de encerramento da assinatura não pode ser anterior à data de entrada.');
      return false;
    }

    const nova = {
      id: editorAssinatura.id,
      servico: editorAssinatura.servico,
      situacao: editorAssinatura.situacao,
      dataDisponibilidade: editorAssinatura.dataDisponibilidade || null,
      dataEncerramento: editorAssinatura.dataEncerramento || null
    };

    const atualizadas = [
      ...assinaturas.filter((item) => item.servico !== nova.servico),
      nova
    ].sort((a, b) => a.servico.localeCompare(b.servico, 'pt-BR'));

    setAssinaturas(atualizadas);
    setAssinaturasEditadasPeloUsuario(true);
    setEditorAssinatura(null);
    setErro('');
    return atualizadas;
  };

  const removerAssinaturaDaLista = (assinatura) => {
    setAssinaturas((atuais) => atuais.filter((item) => item.servico !== assinatura.servico));
    if (assinatura.id) {
      setAssinaturasRemovidas((atuais) => [...atuais, assinatura.id]);
    }
    setAssinaturasEditadasPeloUsuario(true);
    setErro('');
  };

  const validar = (assinaturasEfetivas) => {
    if (!titulo.trim()) return 'Informe o nome do jogo.';
    if (Object.keys(selecionadas).length === 0) return 'Selecione pelo menos uma plataforma.';
    if (anoLancamento !== '' && (Number(anoLancamento) < 1 || Number(anoLancamento) > 9999)) {
      return 'Informe um ano de lançamento válido.';
    }

    for (const [plataforma, dados] of Object.entries(selecionadas)) {
      if (!dados.compraDigital && !dados.midiaFisica && !dados.porAssinatura) {
        return `Selecione pelo menos uma forma de acesso para ${plataforma}.`;
      }
      if (!dados.acompanhar) continue;
      const horas = numeroOpcional(dados.tempoEmHoras);
      const possiveis = numeroOpcional(dados.conquistasPossiveis);
      const obtidas = numeroOpcional(dados.conquistasObtidas);
      if (horas < 0 || possiveis < 0 || obtidas < 0) return 'Horas e conquistas não podem ser negativas.';
      if (obtidas !== undefined && possiveis === undefined) return 'Informe as conquistas possíveis.';
      if (obtidas > possiveis) return 'Conquistas obtidas não podem superar as possíveis.';
    }

    if (temAlgumaAssinaturaMarcada && assinaturasEfetivas.length === 0) {
      return 'Informe pelo menos uma assinatura ou desmarque a opção Assinatura.';
    }

    for (const item of assinaturasEfetivas) {
      if (item.dataDisponibilidade && item.dataEncerramento && item.dataEncerramento < item.dataDisponibilidade) {
        return 'A data de encerramento da assinatura não pode ser anterior à data de entrada.';
      }
    }

    return '';
  };

  const montarAcompanhamento = (dados) => {
    const acompanhamento = {};
    if (dados.status) acompanhamento.status = dados.status;
    if (dados.dataStatus) acompanhamento.dataStatus = dados.dataStatus;
    if (dados.tempoEmHoras !== '') acompanhamento.tempoEmHoras = Number(dados.tempoEmHoras);
    if (dados.conquistasPossiveis !== '') acompanhamento.conquistasPossiveis = Number(dados.conquistasPossiveis);
    if (dados.conquistasObtidas !== '') acompanhamento.conquistasObtidas = Number(dados.conquistasObtidas);
    return acompanhamento;
  };

  const enviar = async (evento) => {
    evento.preventDefault();
    let assinaturasEfetivas = assinaturas;
    if (editorAssinatura) {
      const resultadoRascunho = confirmarRascunhoAssinatura();
      if (resultadoRascunho === false) return;
      assinaturasEfetivas = resultadoRascunho;
    }

    const mensagemErro = validar(assinaturasEfetivas);
    if (mensagemErro) {
      setErro(mensagemErro);
      return;
    }

    const payload = {
      titulo: titulo.trim(),
      ...(anoLancamento === '' ? {} : { anoLancamento: Number(anoLancamento) }),
      plataformas: opcoesPlataforma
        .filter((plataforma) => selecionadas[plataforma])
        .map((plataforma) => {
          const dados = selecionadas[plataforma];
          return {
            plataforma,
            compraDigital: dados.compraDigital,
            midiaFisica: dados.midiaFisica,
            porAssinatura: dados.porAssinatura,
            ...(dados.acompanhar ? { acompanhamento: montarAcompanhamento(dados) } : {})
          };
        }),
      ...(assinaturasEfetivas.length > 0
        ? {
            assinaturas: assinaturasEfetivas.map((item) => ({
              servico: item.servico,
              situacao: item.situacao,
              dataDisponibilidade: item.dataDisponibilidade ? String(item.dataDisponibilidade).slice(0, 10) : null,
              dataEncerramento: item.dataEncerramento ? String(item.dataEncerramento).slice(0, 10) : null
            }))
          }
        : {})
    };

    try {
      await onSubmit(payload);
      if (assinaturasRemovidas.length > 0) {
        await Promise.allSettled(assinaturasRemovidas.map((id) => excluirAssinatura(id)));
      }
    } catch {
      return;
    }

    if (!editando) {
      setTitulo('');
      setAnoLancamento('');
      setSelecionadas({});
      setAssinaturas([]);
      setAssinaturasRemovidas([]);
      setEditorAssinatura(null);
      setAssinaturasEditadasPeloUsuario(false);
    }
    setErro('');
  };

  return (
    <form className="library-game-form" onSubmit={enviar}>
      <div className="library-game-main-fields">
        <label htmlFor="titulo">Nome do jogo</label>
        <input
          id="titulo"
          value={titulo}
          onChange={(evento) => setTitulo(evento.target.value)}
          onBlur={carregarAssinaturasCompartilhadas}
          maxLength="200"
          placeholder="Ex.: Graveyard Keeper"
          autoComplete="off"
          disabled={saving || editando}
        />
        <label htmlFor="ano-lancamento">Ano de lançamento (opcional)</label>
        <input
          id="ano-lancamento"
          type="number"
          min="1"
          max="9999"
          value={anoLancamento}
          onChange={(evento) => setAnoLancamento(evento.target.value)}
          disabled={saving}
        />
      </div>

      <fieldset className="platform-selector">
        <legend>Plataformas</legend>
        <p>{editando
          ? 'As plataformas já cadastradas estão preservadas. Marque também os outros lugares onde você acessa o jogo.'
          : 'Selecione todas as plataformas onde você acessa ou possui o jogo.'}</p>
        <div className="platform-options">
          {opcoesPlataforma.map((plataforma) => (
            <label key={plataforma} className={selecionadas[plataforma] ? 'selected' : ''}>
              <input
                type="checkbox"
                aria-label={`Selecionar ${plataforma}`}
                checked={Boolean(selecionadas[plataforma])}
                onChange={(evento) => alternarPlataforma(plataforma, evento.target.checked)}
                disabled={saving || plataformasOriginais.has(chavePlataforma(plataforma))}
              />
              <span>{plataforma}</span>
            </label>
          ))}
        </div>
      </fieldset>

      <div className="selected-platforms">
        {opcoesPlataforma.filter((plataforma) => selecionadas[plataforma]).map((plataforma) => {
          const dados = selecionadas[plataforma];
          return (
            <fieldset className="platform-details" key={plataforma}>
              <legend>{plataforma}</legend>
              <div className="access-modes-group" role="group" aria-label={`Formas de acesso em ${plataforma}`}>
                <span className="access-modes-label">Formas de acesso</span>
                <div className="platform-toggles">
                  <label>
                    <input
                      type="checkbox"
                      aria-label={`Compra digital em ${plataforma}`}
                      checked={dados.compraDigital}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'compraDigital', evento.target.checked)}
                      disabled={saving}
                    />
                    Compra digital
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      aria-label={`Mídia física em ${plataforma}`}
                      checked={dados.midiaFisica}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'midiaFisica', evento.target.checked)}
                      disabled={saving}
                    />
                    Mídia física
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      aria-label={`Assinatura em ${plataforma}`}
                      checked={dados.porAssinatura}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'porAssinatura', evento.target.checked)}
                      disabled={saving}
                    />
                    Assinatura
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      aria-label={`Acompanhar progresso em ${plataforma}`}
                      checked={dados.acompanhar}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'acompanhar', evento.target.checked)}
                      disabled={saving}
                    />
                    Acompanhar progresso
                  </label>
                </div>
              </div>

              {dados.acompanhar && (
                <div className="tracking-fields">
                  <label>
                    Status em {plataforma}
                    <select
                      aria-label={`Status em ${plataforma}`}
                      value={dados.status}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'status', evento.target.value)}
                      disabled={saving}
                    >
                      <option value="">Não informado</option>
                      <option value="NaoTerminado">Em andamento</option>
                      <option value="Zerado">Zerado</option>
                      <option value="Dropado">Dropado</option>
                    </select>
                  </label>
                  <label>
                    Data do status em {plataforma}
                    <input
                      aria-label={`Data do status em ${plataforma}`}
                      type="date"
                      value={dados.dataStatus}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'dataStatus', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Horas jogadas em {plataforma}
                    <input
                      aria-label={`Horas jogadas em ${plataforma}`}
                      type="number"
                      min="0"
                      step="0.1"
                      value={dados.tempoEmHoras}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'tempoEmHoras', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Conquistas possíveis em {plataforma}
                    <input
                      aria-label={`Conquistas possíveis em ${plataforma}`}
                      type="number"
                      min="0"
                      value={dados.conquistasPossiveis}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'conquistasPossiveis', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                  <label>
                    Conquistas obtidas em {plataforma}
                    <input
                      aria-label={`Conquistas obtidas em ${plataforma}`}
                      type="number"
                      min="0"
                      value={dados.conquistasObtidas}
                      onChange={(evento) => atualizarPlataforma(plataforma, 'conquistasObtidas', evento.target.value)}
                      disabled={saving}
                    />
                  </label>
                </div>
              )}
            </fieldset>
          );
        })}
      </div>

      {exibirSecaoAssinaturas && (
        <fieldset className="platform-details library-subscriptions-box">
          <legend>Assinaturas vinculadas ao jogo</legend>
          <p className="subscription-hint">
            As assinaturas são compartilhadas automaticamente entre Biblioteca e Lista de Desejos.
          </p>

          {assinaturas.length > 0 && (
            <div className="subscription-badges" aria-label="Assinaturas cadastradas para o jogo">
              {assinaturas.map((assinatura) => {
                const entrada = formatarDataAssinatura(assinatura.dataDisponibilidade);
                const encerramento = formatarDataAssinatura(assinatura.dataEncerramento);
                const meta = [entrada ? `Entrada: ${entrada}` : null, encerramento ? `Encerramento: ${encerramento}` : null]
                  .filter(Boolean)
                  .join(' · ');
                return (
                  <span key={assinatura.servico} className="subscription-badge" title={meta || undefined}>
                    <span className="subscription-badge-label">{formatarBadgeAssinatura(assinatura)}</span>
                    <button
                      type="button"
                      className="badge-icon-btn"
                      aria-label={`Editar assinatura ${assinatura.servico}`}
                      onClick={() => setEditorAssinatura(criarRascunhoAssinatura(assinatura))}
                      disabled={saving}
                    >
                      Editar
                    </button>
                    <button
                      type="button"
                      className="badge-icon-btn danger"
                      aria-label={`Remover assinatura ${assinatura.servico}`}
                      onClick={() => removerAssinaturaDaLista(assinatura)}
                      disabled={saving}
                    >
                      Remover
                    </button>
                  </span>
                );
              })}
            </div>
          )}

          {editorAssinatura ? (
            <div className="subscription-subform">
              <label htmlFor="bib-assinatura-servico">
                Serviço de assinatura
                <select
                  id="bib-assinatura-servico"
                  value={editorAssinatura.servico}
                  onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, servico: evento.target.value })}
                  disabled={saving}
                >
                  {SERVICOS_ASSINATURA.map((opcao) => (
                    <option key={opcao} value={opcao}>{opcao}</option>
                  ))}
                </select>
              </label>

              <label htmlFor="bib-assinatura-situacao">
                Situação da disponibilidade
                <select
                  id="bib-assinatura-situacao"
                  value={editorAssinatura.situacao}
                  onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, situacao: evento.target.value })}
                  disabled={saving}
                >
                  {SITUACOES_ASSINATURA.map((opcao) => (
                    <option key={opcao} value={opcao}>{opcao}</option>
                  ))}
                </select>
              </label>

              <label htmlFor="bib-assinatura-entrada">
                Data de entrada
                <input
                  id="bib-assinatura-entrada"
                  type="date"
                  value={editorAssinatura.dataDisponibilidade}
                  onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, dataDisponibilidade: evento.target.value })}
                  disabled={saving}
                />
              </label>

              <label htmlFor="bib-assinatura-encerramento">
                Data de encerramento
                <input
                  id="bib-assinatura-encerramento"
                  type="date"
                  value={editorAssinatura.dataEncerramento}
                  onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, dataEncerramento: evento.target.value })}
                  disabled={saving}
                />
              </label>

              <div className="subscription-subform-actions">
                <button
                  type="button"
                  className="secondary-button"
                  onClick={() => setEditorAssinatura(null)}
                  disabled={saving}
                >
                  Fechar
                </button>
                <button
                  type="button"
                  onClick={confirmarRascunhoAssinatura}
                  disabled={saving}
                >
                  Confirmar assinatura
                </button>
              </div>
            </div>
          ) : (
            <button
              type="button"
              className="compact-action-btn"
              onClick={() => setEditorAssinatura(criarRascunhoAssinatura())}
              disabled={saving}
            >
              Adicionar assinatura
            </button>
          )}
        </fieldset>
      )}

      {erro && <div className="feedback erro" role="alert">{erro}</div>}

      <div className="library-form-actions">
        {editando && <button type="button" className="secondary-button" onClick={onCancel} disabled={saving}>Cancelar</button>}
        <button type="submit" disabled={saving}>
          {saving ? (editando ? 'Salvando...' : 'Adicionando...') : (editando ? 'Salvar alterações' : 'Adicionar à biblioteca')}
        </button>
      </div>
    </form>
  );
}

export default LibraryGameForm;
